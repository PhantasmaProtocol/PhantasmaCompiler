using System;
using System.Collections.Generic;

namespace Phantasma.CodeGen.Core
{
    public class ParserException: Exception
    {
        public enum Kind
        {
            EndOfStream,
            UnexpectedToken,
            ExpectedToken,
            ExpectedIdentifier,
            ExpectedLiteral,
            ExpectedOperator,
            ExpectedKeyword,
            DuplicatedLabel
        }

        public readonly Token token;
        public readonly Kind kind;

        public ParserException(Token token, Kind kind)
        {
            this.token = token;
            this.kind = kind;
        }
    }

    public abstract class Parser
    {
        public abstract ModuleNode Execute(List<Token> tokens);
    }

}
