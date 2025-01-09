using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace TSQLSmellSCA
{
    public static class FragmentTypeParser
    {
        public static string GetFragmentType(TSqlFragment Statement)
        {
            var Type = Statement.ToString();
            var TypeSplit = Type.Split('.');
            var StmtType = TypeSplit[TypeSplit.Length - 1];
            return (StmtType);
        }
    }
}