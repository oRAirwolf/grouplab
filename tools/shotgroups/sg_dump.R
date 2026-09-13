#!/usr/bin/env Rscript
## ---------------------------------------------------------------------------
## shotGroups -> flat numeric dump for cross-implementation validation.
## Emits ONE tidy CSV (long format) + ONE JSON with identical content.
## Stable key scheme:  <function>.<component>.<row>.<col>
## Usage: Rscript sg_dump.R [DATASET] [OUTSTEM]
## ---------------------------------------------------------------------------
suppressMessages(library(shotGroups))

args    <- commandArgs(trailingOnly = TRUE)
dsName  <- if (length(args) >= 1) args[1] else "DF300BLK"
outStem <- if (length(args) >= 2) args[2] else paste0("shotGroups_", dsName)

DF <- get(dsName, envir = asNamespace("shotGroups"))
xy <- getXYmat(DF, xyTopLeft = TRUE, relPOA = FALSE, center = FALSE)

## ---- accumulator ----------------------------------------------------------
rows <- list()
add  <- function(fn, comp, value, row = NA_character_, col = NA_character_) {
  rows[[length(rows) + 1L]] <<- data.frame(
    fn = fn, component = comp, row = row, col = col,
    value = as.numeric(value), stringsAsFactors = FALSE)
}
## flatten any numeric scalar / named vector / matrix / nested list
flatten <- function(fn, comp, x) {
  if (is.null(x)) return(invisible(NULL))
  if (is.matrix(x)) {
    rn <- rownames(x); cn <- colnames(x)
    if (is.null(rn)) rn <- as.character(seq_len(nrow(x)))
    if (is.null(cn)) cn <- as.character(seq_len(ncol(x)))
    for (i in seq_len(nrow(x))) for (j in seq_len(ncol(x)))
      add(fn, comp, x[i, j], rn[i], cn[j])
  } else if (is.list(x)) {
    nms <- names(x); if (is.null(nms)) nms <- as.character(seq_along(x))
    for (k in seq_along(x)) flatten(fn, paste(comp, nms[k], sep = "."), x[[k]])
  } else if (is.numeric(x)) {
    nms <- names(x)
    if (is.null(nms)) nms <- if (length(x) == 1L) NA_character_ else as.character(seq_along(x))
    for (k in seq_along(x)) add(fn, comp, x[k], NA_character_, nms[k])
  }
  invisible(NULL)
}

## ---- metadata -------------------------------------------------------------
add("meta", "nShots",  nrow(xy))
add("meta", "distance", unique(DF$distance)[1])

## ---- descriptive / spread -------------------------------------------------
gs <- groupSpread(DF, center = FALSE, plots = FALSE, CEPlevel = 0.5,
                  CIlevel = 0.95, CEPtype = "CorrNormal", bootCI = "none")
for (n in names(gs)) flatten("groupSpread", n, gs[[n]])

## ---- location -------------------------------------------------------------
gl <- groupLocation(DF, level = 0.95, plots = FALSE, bootCI = "none")
for (n in setdiff(names(gl), "Hotelling")) flatten("groupLocation", n, gl[[n]])
add("groupLocation", "Hotelling.statistic", gl$Hotelling[1, "approx F"])
add("groupLocation", "Hotelling.p.value",   gl$Hotelling[1, "Pr(>F)"])

## ---- shape ----------------------------------------------------------------
gsh <- groupShape(DF, center = FALSE, plots = FALSE, bandW = 0.5, outlier = "mcd")
flatten("groupShape", "corXY",    gsh$corXY)
flatten("groupShape", "corXYrob", gsh$corXYrob)
for (t in c("ShapiroX", "ShapiroY", "multNorm")) {
  add("groupShape", paste0(t, ".statistic"), gsh[[t]]$statistic)
  add("groupShape", paste0(t, ".p.value"),   gsh[[t]]$p.value)
}

## ---- CEP: every estimator, several levels ---------------------------------
CEPtypes <- c("CorrNormal","GrubbsPatnaik","GrubbsPearson","GrubbsLiu","Rayleigh",
              "Krempasky","Ignani","RMSE","Ethridge","RAND","Valstar")
for (acc in c(FALSE, TRUE)) {
  cep <- suppressWarnings(
    getCEP(xy, CEPlevel = c(0.5, 0.9, 0.95), type = CEPtypes, accuracy = acc))
  flatten(paste0("getCEP.accuracy_", acc), "CEP", cep$CEP)
  flatten(paste0("getCEP.accuracy_", acc), "ellShape", cep$ellShape)
  flatten(paste0("getCEP.accuracy_", acc), "ctr", cep$ctr)
}

## ---- distribution parameters ----------------------------------------------
flatten("getRayParam",  "", getRayParam(xy, level = 0.95))
flatten("getRiceParam", "", getRiceParam(xy, level = 0.95))
flatten("getHoytParam", "", getHoytParam(cov(xy)))

## ---- hit probability ------------------------------------------------------
flatten("getHitProb", "r",
        suppressWarnings(getHitProb(xy, r = c(0.5, 1, 2), unit = "unit",
                                    type = CEPtypes, accuracy = FALSE)))

## ---- geometry -------------------------------------------------------------
bb <- getBoundingBox(xy)
flatten("getBoundingBox", "pts", bb$pts)
for (k in c("width","height","FoM","diag")) add("getBoundingBox", k, bb[[k]])
mb <- getMinBBox(xy)
flatten("getMinBBox", "pts", mb$pts)
for (k in c("width","height","FoM","diag","angle")) add("getMinBBox", k, mb[[k]])
mc <- getMinCircle(xy);  flatten("getMinCircle", "ctr", mc$ctr); add("getMinCircle","rad", mc$rad)
me <- getMinEllipse(xy); flatten("getMinEllipse","ctr", me$ctr)
flatten("getMinEllipse","shape", me$shape); flatten("getMinEllipse","size", me$size)
add("getMinEllipse","area", me$area)
mp <- getMaxPairDist(xy); add("getMaxPairDist","d", mp$d)
add("getMaxPairDist","idx1", mp$idx[1]); add("getMaxPairDist","idx2", mp$idx[2])
ce <- getConfEll(xy, level = 0.5, doRob = TRUE)
for (k in c("ctr","ctrRob","cov","covRob","size","sizeRob","shape","shapeRob"))
  flatten("getConfEll", k, ce[[k]])
add("getConfEll","magFac", ce$magFac)

## per-shot radial distances (indexed by shot number)
dtc <- getDistToCtr(xy)
for (i in seq_along(dtc)) add("getDistToCtr", "r", dtc[i], col = as.character(i))

## ---- range statistics -----------------------------------------------------
rs <- getRangeStat(DF)
flatten("getRangeStat", "range_stat", rs$range_stat)
flatten("getRangeStat", "CI", rs$CI)
n <- nrow(xy)
flatten("range2sigma", "", range2sigma(x = rs$range_stat["unit","ES"], stat = "ES",
                                       n = n, nGroups = 1))
flatten("range2CEP",   "", range2CEP(x = rs$range_stat["unit","ES"], stat = "ES",
                                     n = n, nGroups = 1, CEPlevel = 0.5))
eff <- getRangeStatEff(n = n, nGroups = 1)
for (cl in names(eff)) add("getRangeStatEff", cl, eff[[cl]])

## ---- angular conversion round-trip ----------------------------------------
dst <- unique(DF$distance)[1]
cnv <- paste0(unique(as.character(DF$distance.unit))[1], "2",
              unique(as.character(DF$point.unit))[1])
for (ty in c("deg","rad","MOA","SMOA","mrad","mil")) {
  add("getMOA",  ty, getMOA(1, dst = dst, conversion = cnv, type = ty))
  add("fromMOA", ty, fromMOA(1, dst = dst, conversion = cnv, type = ty))
}

## ---- write ----------------------------------------------------------------
out <- do.call(rbind, rows)
out$key <- paste(out$fn, out$component,
                 ifelse(is.na(out$row), "", out$row),
                 ifelse(is.na(out$col), "", out$col), sep = ".")
out$key <- gsub("\\.+", ".", sub("\\.+$", "", out$key))
out <- out[, c("key","fn","component","row","col","value")]

write.csv(out, paste0(outStem, ".csv"), row.names = FALSE)
if (requireNamespace("jsonlite", quietly = TRUE)) {
  writeLines(jsonlite::toJSON(
    list(package = "shotGroups",
         version = as.character(packageVersion("shotGroups")),
         dataset = dsName, nShots = nrow(xy),
         R = R.version.string,
         values = setNames(as.list(out$value), out$key)),
    auto_unbox = TRUE, digits = 15, na = "null", pretty = TRUE),
    paste0(outStem, ".json"))
}
cat("rows written:", nrow(out), "->", paste0(outStem, ".csv/.json"), "\n")
