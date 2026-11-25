#!/bin/sh
# Question 120 (b): says whether the device build just made was compiled with Mono's LLVM code generator, so the build's own log shows it.
# Two signs, each enough: the object files LLVM writes beside the ahead-of-time output (*.llvm.o under the project's obj folder), and the
# LLVM arguments in the build's detailed log. Usage: sh ios/llvm-evidence.sh <detailed log or -> <out file>. It never fails the build;
# when GroupLabAppleLlvm is on and neither sign is there it warns, and the step summary says so.
log="$1"
out="${2:-ios-llvm.txt}"
objs=$(find ios/GroupLab.iOS/obj -path '*ios-arm64*' -name '*.llvm.o' 2>/dev/null | wc -l | tr -d ' ')
args=0
if [ "$log" != "-" ] && [ -f "$log" ]; then
  args=$(grep -c -E 'llvm-outfile=|llvm-path=|--llvm' "$log" || true)
fi
setting=$(grep -o '<GroupLabAppleLlvm Condition="[^"]*">[a-z]*<' ios/GroupLab.iOS/GroupLab.iOS.csproj | sed 's/.*">//; s/<$//')
{
  echo "llvm setting in the project: ${setting:-absent}"
  echo "llvm object files: $objs"
  echo "llvm arguments in the build log: $args"
  if [ "$objs" -gt 0 ] || [ "$args" -gt 0 ]; then echo "llvm: ran"; else echo "llvm: not seen"; fi
  find ios/GroupLab.iOS/obj -path '*ios-arm64*' -name '*.llvm.o' 2>/dev/null | sed 's|.*/||' | sort | head -8
  if [ "$log" != "-" ] && [ -f "$log" ]; then
    grep -o -E '(llvm-outfile|llvm-path)=[^,; ]*' "$log" | sed 's|=.*/|=.../|' | sort -u | head -4
  fi
} > "$out"
cat "$out"
if grep -q "^llvm: ran" "$out"; then
  [ -n "$GITHUB_STEP_SUMMARY" ] && echo "LLVM ran in this Apple build ($objs object files, $args log lines)." >> "$GITHUB_STEP_SUMMARY"
elif [ "$setting" = "true" ]; then
  echo "::warning::GroupLabAppleLlvm is on but this Apple build shows no sign that LLVM ran; ios-llvm.txt has what was looked for."
  [ -n "$GITHUB_STEP_SUMMARY" ] && echo "LLVM was asked for and not seen in this Apple build." >> "$GITHUB_STEP_SUMMARY"
fi
exit 0
