#!/usr/bin/env Rscript
## ---------------------------------------------------------------------------
## shotGroups -> flat numeric dump for cross-implementation validation.
## Emits ONE tidy CSV (long format) + ONE JSON with identical content.
##
## Key scheme:  <scope>.<function>.<component>.<row>.<col>
##   scope is omitted for whole-dataset results, so every key produced by the
##   original version of this script is unchanged.  Per-group results carry the
##   scope "series.<label>." in front.  STATISTICS.md section 15.4 point 8: the
##   package keys multi-group data on `series`, not on `group`, and in DFsavage
##   and DFtalon the two disagree.  This script follows `series` throughout and
##   records both labels against every shot so a reader can see the difference.
##
## Sections, and which part of STATISTICS.md section 15 each one serves:
##   shots          the input coordinates AND each shot's point of aim, so a
##                  fixture is self-contained and a reimplementation is not
##                  asked to reconstruct them from the package it is being
##                  validated against.  The aim is what the first version
##                  missed; see the comment on that block
##   whole dataset  the original dump, unchanged
##   per series     15.5 point 1 across all eight fixtures, where a multi-group
##                  dataset previously produced one pooled figure
##   compareGroups  15.4 point 6, including which test branch actually ran
##   angular        15.5 point 3, the negative test: multiple distances in one
##                  frame must suppress angular output rather than produce a
##                  wrong number
##
## Usage: Rscript sg_dump.R [DATASET] [OUTSTEM]
## ---------------------------------------------------------------------------
suppressMessages(library(shotGroups))

args    <- commandArgs(trailingOnly = TRUE)
dsName  <- if (length(args) >= 1) args[1] else "DF300BLK"
outStem <- if (length(args) >= 2) args[2] else paste0("shotGroups_", dsName)

DF <- get(dsName, envir = asNamespace("shotGroups"))
xy <- getXYmat(DF, xyTopLeft = TRUE, relPOA = FALSE, center = FALSE)

HAS_COIN <- requireNamespace("coin", quietly = TRUE)

## groupShape's multivariate-normality test is a Monte Carlo energy test, so its
## p-value moves between two identical runs: measured at 0.5424 and 0.5590 on
## DF300BLK, 0.8346 and 0.8379 on DFcciHV.  Every other value in this dump is
## deterministic, checked by running each dataset twice and diffing.  Seeding
## makes regeneration reproducible; it does NOT make the value comparable,
## because a reimplementation draws from a different generator.  The key is
## listed in the JSON under `stochastic` so a comparison harness can exclude it
## by name rather than by discovering the failure at a 1e-12 tolerance.
set.seed(20260914L)
STOCHASTIC <- "groupShape.multNorm.p.value"

## ---- accumulator ----------------------------------------------------------
rows  <- list()
SCOPE <- ""                      # "" for whole dataset, "series.<label>." otherwise
add   <- function(fn, comp, value, row = NA_character_, col = NA_character_) {
  rows[[length(rows) + 1L]] <<- data.frame(
    scope = SCOPE, fn = fn, component = comp, row = row, col = col,
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
  } else if (is.data.frame(x)) {
    flatten(fn, comp, as.matrix(x[, vapply(x, is.numeric, logical(1)), drop = FALSE]))
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
## Record a section that could not run, rather than aborting the dump.  A
## fixture that silently omits a section looks identical to one whose section
## produced nothing, and the two mean opposite things.
attempt <- function(label, expr) {
  tryCatch(force(expr), error = function(e) {
    add(label, "_error", 1)
    message(sprintf("  [skip] %s%s: %s", SCOPE, label, conditionMessage(e)))
    invisible(NULL)
  })
}

## ---- the input, so the fixture stands on its own --------------------------
## Without this a reimplementation has to obtain the coordinates from the very
## package it is being checked against, which is not an independent test.
grp <- if ("group"  %in% names(DF)) as.character(DF$group)  else rep("1", nrow(DF))
ser <- if ("series" %in% names(DF)) as.character(DF$series) else grp
## The point of aim, which the first version of this script omitted and which
## cost a reimplementation 2,163 keys it could not reproduce.  groupLocation,
## groupSpread and groupShape take the data frame and use each shot's aim;
## getXYmat with relPOA = FALSE does not carry it, so a fixture built only from
## the matrix cannot reproduce anything those three computed.  It shows up as
## the frame-based centre disagreeing with the matrix-based one, in every scope
## of DFcm and DFinch and in none of the other seven datasets, because only
## those two have a non-zero aim: 20.4 cm and 8.0 in at the extreme.
##
## Both forms are emitted rather than one.  xyTopLeft = TRUE flips y, so a
## reader deriving one from the other has a sign convention to get right, and
## that is exactly the kind of thing that costs a day.
xyPOA <- getXYmat(DF, xyTopLeft = TRUE, relPOA = TRUE, center = FALSE)
for (i in seq_len(nrow(xy))) {
  add("shots", "x",        xy[i, 1],    col = as.character(i))
  add("shots", "y",        xy[i, 2],    col = as.character(i))
  add("shots", "xPOA",     xyPOA[i, 1], col = as.character(i))
  add("shots", "yPOA",     xyPOA[i, 2], col = as.character(i))
  add("shots", "distance", DF$distance[i], col = as.character(i))
}
## Labels are not numeric, so they travel as a factor index plus a level table.
serLev <- sort(unique(ser)); grpLev <- sort(unique(grp))
for (i in seq_len(nrow(xy))) {
  add("shots", "seriesIndex", match(ser[i], serLev), col = as.character(i))
  add("shots", "groupIndex",  match(grp[i], grpLev), col = as.character(i))
}

## ---- metadata -------------------------------------------------------------
add("meta", "distance",      unique(DF$distance)[1])
add("meta", "nDistances",    length(unique(DF$distance)))
add("meta", "nSeries",       length(serLev))
add("meta", "nGroups",       length(grpLev))
## STATISTICS.md 15.4 point 6: compareGroups switches between exact permutation
## and asymptotic tests depending on whether coin is installed, so the branch is
## part of the fixture and not an incidental fact about the machine.
add("meta", "coinInstalled", as.numeric(HAS_COIN))
## groupShape's robust branch reaches for mvoutlier when it is present.  Its
## absence changes nothing this gate compares, but it changes the warning a
## reader sees when regenerating, so it is recorded rather than left to puzzle.
add("meta", "mvoutlierInstalled",
    as.numeric(requireNamespace("mvoutlier", quietly = TRUE)))

## ---------------------------------------------------------------------------
## Everything one scope needs.  Called once for the whole dataset and once per
## series.  Identical code both times, so a per-series figure and a pooled one
## are never produced by two different paths.
## ---------------------------------------------------------------------------
dumpScope <- function(DFs) {
  xys <- getXYmat(DFs, xyTopLeft = TRUE, relPOA = FALSE, center = FALSE)
  n   <- nrow(xys)
  add("meta", "nShots", n)

  attempt("groupSpread", {
    gs <- groupSpread(DFs, center = FALSE, plots = FALSE, CEPlevel = 0.5,
                      CIlevel = 0.95, CEPtype = "CorrNormal", bootCI = "none")
    for (nm in names(gs)) flatten("groupSpread", nm, gs[[nm]])
  })

  attempt("groupLocation", {
    gl <- groupLocation(DFs, level = 0.95, plots = FALSE, bootCI = "none")
    for (nm in setdiff(names(gl), "Hotelling")) flatten("groupLocation", nm, gl[[nm]])
    add("groupLocation", "Hotelling.statistic", gl$Hotelling[1, "approx F"])
    add("groupLocation", "Hotelling.p.value",   gl$Hotelling[1, "Pr(>F)"])
  })

  attempt("groupShape", {
    gsh <- groupShape(DFs, center = FALSE, plots = FALSE, bandW = 0.5, outlier = "mcd")
    flatten("groupShape", "corXY",    gsh$corXY)
    flatten("groupShape", "corXYrob", gsh$corXYrob)
    for (t in c("ShapiroX", "ShapiroY", "multNorm")) {
      add("groupShape", paste0(t, ".statistic"), gsh[[t]]$statistic)
      add("groupShape", paste0(t, ".p.value"),   gsh[[t]]$p.value)
    }
  })

  ## CEP: every estimator, several levels.  STATISTICS.md 15.4 point 4 warns
  ## that the output column order differs from the `type` argument order, so
  ## everything here is keyed by name and never by position.
  CEPtypes <- c("CorrNormal","GrubbsPatnaik","GrubbsPearson","GrubbsLiu","Rayleigh",
                "Krempasky","Ignani","RMSE","Ethridge","RAND","Valstar")
  for (acc in c(FALSE, TRUE)) {
    attempt(paste0("getCEP.accuracy_", acc), {
      cep <- suppressWarnings(
        getCEP(xys, CEPlevel = c(0.5, 0.9, 0.95), type = CEPtypes, accuracy = acc))
      flatten(paste0("getCEP.accuracy_", acc), "CEP",      cep$CEP)
      flatten(paste0("getCEP.accuracy_", acc), "ellShape", cep$ellShape)
      flatten(paste0("getCEP.accuracy_", acc), "ctr",      cep$ctr)
    })
  }

  attempt("getRayParam",  flatten("getRayParam",  "", getRayParam(xys, level = 0.95)))
  attempt("getRiceParam", flatten("getRiceParam", "", getRiceParam(xys, level = 0.95)))
  attempt("getHoytParam", flatten("getHoytParam", "", getHoytParam(cov(xys))))

  attempt("getHitProb", flatten("getHitProb", "r",
    suppressWarnings(getHitProb(xys, r = c(0.5, 1, 2), unit = "unit",
                                type = CEPtypes, accuracy = FALSE))))

  ## ---- geometry -----------------------------------------------------------
  attempt("getBoundingBox", {
    bb <- getBoundingBox(xys)
    flatten("getBoundingBox", "pts", bb$pts)
    for (k in c("width","height","FoM","diag")) add("getBoundingBox", k, bb[[k]])
  })
  attempt("getMinBBox", {
    mb <- getMinBBox(xys)
    flatten("getMinBBox", "pts", mb$pts)
    for (k in c("width","height","FoM","diag","angle")) add("getMinBBox", k, mb[[k]])
  })
  attempt("getMinCircle", {
    mc <- getMinCircle(xys); flatten("getMinCircle", "ctr", mc$ctr)
    add("getMinCircle", "rad", mc$rad)
  })
  attempt("getMinEllipse", {
    me <- getMinEllipse(xys); flatten("getMinEllipse", "ctr", me$ctr)
    flatten("getMinEllipse", "shape", me$shape); flatten("getMinEllipse", "size", me$size)
    add("getMinEllipse", "area", me$area)
  })
  attempt("getMaxPairDist", {
    mp <- getMaxPairDist(xys); add("getMaxPairDist", "d", mp$d)
    add("getMaxPairDist", "idx1", mp$idx[1]); add("getMaxPairDist", "idx2", mp$idx[2])
  })
  attempt("getConfEll", {
    ce <- getConfEll(xys, level = 0.5, doRob = TRUE)
    for (k in c("ctr","ctrRob","cov","covRob","size","sizeRob","shape","shapeRob"))
      flatten("getConfEll", k, ce[[k]])
    add("getConfEll", "magFac", ce$magFac)
  })
  attempt("getDistToCtr", {
    dtc <- getDistToCtr(xys)
    for (i in seq_along(dtc)) add("getDistToCtr", "r", dtc[i], col = as.character(i))
  })

  ## ---- range statistics ---------------------------------------------------
  ## Each of these is attempted separately.  getRangeStat itself works at any n,
  ## warning and returning NA intervals past the table, while range2sigma,
  ## range2CEP and getRangeStatEff raise an error above n = 100, the largest
  ## cell shotGroups tabulates.  Wrapping them together threw away three working
  ## results whenever the fourth was out of range, which is what happened on the
  ## pooled scope of every dataset over 100 shots.
  ## Assigned by value rather than through `attempt`, because `attempt`
  ## evaluates its argument as a promise in this frame and a `<<-` inside it
  ## walks past this frame to the global environment, leaving the local NULL.
  rangeStat <- tryCatch(suppressWarnings(getRangeStat(DFs)),
                        error = function(e) { add("getRangeStat", "_error", 1); NULL })
  if (!is.null(rangeStat)) {
    flatten("getRangeStat", "range_stat", rangeStat$range_stat)
    flatten("getRangeStat", "CI", rangeStat$CI)
    es <- rangeStat$range_stat["unit", "ES"]
    attempt("range2sigma", flatten("range2sigma", "",
      range2sigma(x = es, stat = "ES", n = n, nGroups = 1)))
    attempt("range2CEP", flatten("range2CEP", "",
      range2CEP(x = es, stat = "ES", n = n, nGroups = 1, CEPlevel = 0.5)))
  }
  attempt("getRangeStatEff", {
    eff <- getRangeStatEff(n = n, nGroups = 1)
    for (cl in names(eff)) add("getRangeStatEff", cl, eff[[cl]])
  })

  ## ---- angular conversion -------------------------------------------------
  ## STATISTICS.md 15.5 point 3.  With more than one distance in the frame there
  ## is no single conversion, and the right behaviour is to emit nothing.  The
  ## count is recorded either way so the absence is positive evidence.
  dsts <- unique(DFs$distance)
  add("angular", "nDistances", length(dsts))
  if (length(dsts) == 1L) {
    cnv <- paste0(unique(as.character(DFs$distance.unit))[1], "2",
                  unique(as.character(DFs$point.unit))[1])
    for (ty in c("deg","rad","MOA","SMOA","mrad","mil")) {
      add("getMOA",  ty, getMOA(1,  dst = dsts[1], conversion = cnv, type = ty))
      add("fromMOA", ty, fromMOA(1, dst = dsts[1], conversion = cnv, type = ty))
    }
  }
}

## ---- whole dataset --------------------------------------------------------
message("[", dsName, "] whole dataset, n = ", nrow(xy))
dumpScope(DF)

## ---- per series -----------------------------------------------------------
## groupSpread and friends pool every shot handed to them, so on a multi-group
## dataset the whole-dataset figures above describe a group that was never
## fired.  They are kept because they are what the original script produced and
## because the pooled case is itself exercised by section 11 of STATISTICS.md;
## these are the per-group figures the gate actually needs.
if (length(serLev) > 1L) {
  for (lab in serLev) {
    SCOPE <- paste0("series.", lab, ".")
    sub   <- DF[ser == lab, , drop = FALSE]
    message("[", dsName, "] series ", lab, ", n = ", nrow(sub))
    dumpScope(droplevels(sub))
  }
  SCOPE <- ""
}

## ---- multi-group range statistics -----------------------------------------
## STATISTICS.md 15.2 chose DFlandy01 for "range statistics with many groups",
## but getRangeStat takes a coordinate matrix and has no group argument: handed
## a 53-group frame it pools all 530 shots, warns that it is past the table, and
## returns NA intervals.  The package's actual multi-group path is the nGroups
## argument of range2sigma, range2CEP and getRangeStatEff.  Those are emitted
## here at the real group count, which is what that fixture was chosen to
## exercise and what the pooled call above cannot reach.
if (length(serLev) > 1L) {
  nPer <- as.numeric(names(sort(table(table(ser)), decreasing = TRUE))[1])
  add("multiGroup", "nGroups",     length(serLev))
  add("multiGroup", "nPerGroup",   nPer)
  add("multiGroup", "groupsEqual", as.numeric(length(unique(table(ser))) == 1L))
  if (!is.na(nPer) && nPer >= 2 && nPer <= 100 && length(serLev) <= 10) {
    attempt("multiGroup.getRangeStatEff", {
      eff <- getRangeStatEff(n = nPer, nGroups = length(serLev))
      for (cl in names(eff)) add("multiGroup.getRangeStatEff", cl, eff[[cl]])
    })
    attempt("multiGroup.range2sigma", flatten("multiGroup.range2sigma", "",
      range2sigma(x = 1, stat = "ES", n = nPer, nGroups = length(serLev))))
    attempt("multiGroup.range2CEP", flatten("multiGroup.range2CEP", "",
      range2CEP(x = 1, stat = "ES", n = nPer, nGroups = length(serLev),
                CEPlevel = 0.5)))
  } else {
    ## Above ten groups shotGroups tabulates nothing, so the absence is the
    ## result.  DFlandy01 at 53 groups lands here, which is worth knowing
    ## before anyone builds a gate around it.
    add("multiGroup", "beyondTable", 1)
  }
}

## ---- group comparison -----------------------------------------------------
## STATISTICS.md 15.2: DFcciHV exercises the two-group Ansari-Bradley and
## Wilcoxon branch, DF300BLKhl the multi-group Fligner-Killeen and
## Kruskal-Wallis branch.  Two-group results are coin S4 objects and
## multi-group results are plain htest lists, so each needs its own extractor.
statOf <- function(o) {
  if (isS4(o)) as.numeric(coin::statistic(o))
  else if (!is.null(o$statistic)) as.numeric(o$statistic) else NA_real_
}
pOf <- function(o) {
  if (isS4(o)) as.numeric(coin::pvalue(o))
  else if (!is.null(o$p.value)) as.numeric(o$p.value) else NA_real_
}
if (length(serLev) > 1L) {
  attempt("compareGroups", {
    cg <- compareGroups(DF, plots = FALSE, xyTopLeft = TRUE, center = FALSE,
                        CEPtype = "CorrNormal", CEPlevel = 0.5, CIlevel = 0.95)
    for (nm in c("ctr","distPOA","corXY","sdXY","sdXYci","meanDistToCtr",
                 "maxPairDist","bbFoM","bbDiag","minCircleRad","sigma","MR",
                 "sigmaMRci","CEP"))
      if (!is.null(cg[[nm]])) flatten("compareGroups", nm, cg[[nm]])
    if (!is.null(cg$MANOVA)) {
      add("compareGroups", "MANOVA.Wilks",    cg$MANOVA[1, "Wilks"])
      add("compareGroups", "MANOVA.approxF",  cg$MANOVA[1, "approx F"])
      add("compareGroups", "MANOVA.p.value",  cg$MANOVA[1, "Pr(>F)"])
    }
    ## Both branches are probed by name.  Which pair is present is itself the
    ## record of which branch ran, so the names are not assumed.
    for (nm in c("AnsariX","AnsariY","Wilcoxon","FlignerX","FlignerY","Kruskal"))
      if (!is.null(cg[[nm]])) {
        add("compareGroups", paste0(nm, ".statistic"), statOf(cg[[nm]]))
        add("compareGroups", paste0(nm, ".p.value"),   pOf(cg[[nm]]))
      }
  })
}

## ---- write ----------------------------------------------------------------
out <- do.call(rbind, rows)
out$key <- paste0(out$scope,
                  paste(out$fn, out$component,
                        ifelse(is.na(out$row), "", out$row),
                        ifelse(is.na(out$col), "", out$col), sep = "."))
out$key <- gsub("\\.+", ".", sub("\\.+$", "", out$key))
if (any(duplicated(out$key)))
  stop("duplicate keys, the scheme is not unique: ",
       paste(head(unique(out$key[duplicated(out$key)])), collapse = ", "))
out <- out[, c("key","scope","fn","component","row","col","value")]

write.csv(out, paste0(outStem, ".csv"), row.names = FALSE)
if (requireNamespace("jsonlite", quietly = TRUE)) {
  writeLines(jsonlite::toJSON(
    list(package       = "shotGroups",
         version       = as.character(packageVersion("shotGroups")),
         dataset       = dsName,
         nShots        = nrow(xy),
         nSeries       = length(serLev),
         seriesLabels  = serLev,
         groupLabels   = grpLev,
         coinInstalled = HAS_COIN,
         stochastic    = STOCHASTIC,
         R             = R.version.string,
         generated     = format(Sys.time(), "%Y-%m-%dT%H:%M:%SZ", tz = "UTC"),
         values        = setNames(as.list(out$value), out$key)),
    auto_unbox = TRUE, digits = 15, na = "null", pretty = TRUE),
    paste0(outStem, ".json"))
}
cat("rows written:", nrow(out), "->", paste0(outStem, ".csv/.json"), "\n")
