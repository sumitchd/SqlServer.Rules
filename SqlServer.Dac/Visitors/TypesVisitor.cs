using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;

namespace SqlServer.Dac.Visitors
{
    public class TypesVisitor : BaseVisitor, IVisitor<TSqlFragment>
    {
        private readonly IList<Type> _types = new List<Type>();
        public IList<TSqlFragment> Statements { get; } = new List<TSqlFragment>();
        public int Count { get { return Statements.Count; } }
        public TypesVisitor(params Type[] typesToLookFor)
        {
            if (typesToLookFor.Length == 0) { throw new ArgumentNullException(nameof(typesToLookFor)); }
            _types = new List<Type>(typesToLookFor);
        }

        public override void Visit(TSqlFragment node)
        {
            if (_types.Contains(node.GetType()))
            {
                Statements.Add(node);
            }
        }
    }
}
