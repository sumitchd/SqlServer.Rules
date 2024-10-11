# NEW RULE IDEAS

- FKs: check for `NOCHECK`, not for replication
- Constraints: check for:
  - udf usage
  - `NOCHECK`
- ~~@@IDENTITY check~~ DONE IN MS RULE SR0008
- Suggest SCHEMABINDING for functions that do not touch tables
- Detect SQL injection possibilities?
  - Would like to only detect injection opportunies where SQL is being concatenated in from a variable
  - Would also like to build a scanner for .NET code
