using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;

namespace TSQLSmellSCA
{
    public class FragmentTypeParser
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