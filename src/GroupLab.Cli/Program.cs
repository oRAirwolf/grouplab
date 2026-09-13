const string Usage = """
    grouplab <command> [options]

    Commands arrive milestone by milestone during Phase 0a:
      validate, encode, decode, render, library, selftest
    """;

Console.Error.WriteLine(Usage);
return args.Length == 0 ? 1 : 2;
