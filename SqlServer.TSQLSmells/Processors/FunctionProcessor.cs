using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace TSQLSmellSCA
{
    public class FunctionProcessor
    {
        private readonly Smells _smells;

        public FunctionProcessor(Smells smells)
        {
            _smells = smells;
        }

        public void ProcessFunctionCall(FunctionCall FunctionCall)
        {
            if (FunctionCall.OverClause != null)
            {
                if (FunctionCall.OverClause.WindowFrameClause != null)
                {
                    if (FunctionCall.OverClause.WindowFrameClause.WindowFrameType == WindowFrameType.Range)
                    {
                        _smells.SendFeedBack(25, FunctionCall.OverClause.WindowFrameClause);
                    }
                }
                else
                {
                    if (FunctionCall.OverClause.OrderByClause != null)
                    {
                        switch (FunctionCall.FunctionName.Value.ToUpperInvariant())
                        {
                            case "ROW_NUMBER":
                            case "RANK":
                            case "DENSE_RANK":
                            case "NTILE":
                            case "LAG":
                            case "LEAD":
                                break;
                            default:
                                _smells.SendFeedBack(26, FunctionCall.OverClause);
                                break;
                        }
                    }
                }
            }
        }
    }
}