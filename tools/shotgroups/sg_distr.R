#!/usr/bin/env Rscript
## ---------------------------------------------------------------------------
## shotGroups' Monte Carlo range-statistic table, reduced to the columns
## STATISTICS.md section 15.3 actually compares against.
##
## WHAT THIS IS, AND WHAT IT IS NOT.  This is shotGroups' own simulated
## distribution, `DFdistr`, extracted verbatim.  It is included as a comparison
## reference with attribution, exactly as STATISTICS.md section 15.1 requires,
## and it is NOT GroupLab's Monte Carlo table.  GroupLab generates its own, per
## section 15.3, and this file is the thing that generated table is measured
## against.  Nothing in GroupLab may read these numbers as its own reference
## data at runtime; they exist for the test suite.
##
## Columns.  Section 15.3 gates the mean to 0.2 percent and the 2.5 and 97.5
## percent quantiles to 0.5 percent, so those three are taken for each of the
## four range statistics the package tabulates:
##   ES   extreme spread, the largest distance between any two shots
##   FoM  figure of merit, the mean of the bounding box width and height
##   D    the bounding box diagonal
##   RS   the Rayleigh sigma estimate derived from the range statistic
##
## Coverage.  Section 15.3 asks for n from 2 to 50 and nGroups from 1 to 10,
## which is 490 cells and complete in the package with no gaps.  The whole
## table is emitted anyway, out to n = 100, because that upper bound is also
## the reason `getRangeStat` refuses a group larger than 100 shots, which is
## visible in the dataset fixtures as a recorded skip on the pooled scope of
## DFcm, DFinch, DFsavage, DFlandy04 and DFlandy01.
##
## Usage: Rscript sg_distr.R [OUTSTEM]
## ---------------------------------------------------------------------------
suppressMessages(library(shotGroups))

args    <- commandArgs(trailingOnly = TRUE)
outStem <- if (length(args) >= 1) args[1] else "shotGroups_DFdistr"

d <- get("DFdistr", envir = asNamespace("shotGroups"))

stats <- c("ES", "FoM", "D", "RS")
want  <- c("n", "nGroups", "nShots",
           as.vector(t(outer(stats, c("_M", "_Q025", "_Q975"), paste0))))

missing <- setdiff(want, names(d))
if (length(missing))
  stop("DFdistr is missing expected columns: ", paste(missing, collapse = ", "))

out <- d[order(d$nGroups, d$n), want]
rownames(out) <- NULL

inGate <- out$n >= 2 & out$n <= 50 & out$nGroups >= 1 & out$nGroups <= 10
out$inSection15_3Gate <- as.integer(inGate)

write.csv(out, paste0(outStem, ".csv"), row.names = FALSE)

if (requireNamespace("jsonlite", quietly = TRUE)) {
  writeLines(jsonlite::toJSON(
    list(package     = "shotGroups",
         version     = as.character(packageVersion("shotGroups")),
         object      = "DFdistr",
         R           = R.version.string,
         generated   = format(Sys.time(), "%Y-%m-%dT%H:%M:%SZ", tz = "UTC"),
         note        = paste("shotGroups' own Monte Carlo table, included as a",
                             "comparison reference with attribution per",
                             "STATISTICS.md section 15.1. This is not GroupLab's",
                             "table; GroupLab generates its own and is measured",
                             "against this one."),
         gate        = list(section = "15.3",
                            meanTolerance     = 0.002,
                            quantileTolerance = 0.005,
                            nRange            = c(2, 50),
                            nGroupsRange      = c(1, 10),
                            cells             = sum(inGate)),
         statistics  = stats,
         rows        = out),
    auto_unbox = TRUE, digits = 15, na = "null", pretty = TRUE),
    paste0(outStem, ".json"))
}

cat("rows written:", nrow(out), "of which", sum(inGate), "inside the section 15.3 gate",
    "->", paste0(outStem, ".csv/.json"), "\n")
