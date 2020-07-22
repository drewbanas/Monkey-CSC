namespace ast
{
    using System.Collections.Generic;
    using token;

    // The base Node interface
    public interface Node
    {
        string TokenLiteral();
        string String();
    }

    // All statement nodes implement this
    public interface Statement : Node
    {
        void statementNode();
    }

    // All expression nodes implement this
    interface Expression : Node
    {
        void expressionNode();
    }

    struct Program : Node
    {
        public List<Statement> Statements;

        public string TokenLiteral()
        {
            if (this.Statements.Count > 0)
            {
                return this.Statements[0].TokenLiteral();
            }
            else
            {
                return "";
            }
        }

        public string String()
        {
            System.Text.StringBuilder out_ = new System.Text.StringBuilder();
            foreach (Statement s in this.Statements)
            {
                out_.Append(s.String());
            }

            return out_.ToString();
        }
    }

    // Statements
    class LetStatement : Statement
    {
        public token.Token Token; // the token.LET token
        public Identifier Name;
        public Expression Value;

        public void statementNode() { }
        public string TokenLiteral() { return this.Token.Literal; }
        public string String()
        {
            System.Text.StringBuilder out_ = new System.Text.StringBuilder();

            out_.Append(this.TokenLiteral() + " ");
            out_.Append(this.Name.String());
            out_.Append(" = ");

            if (this.Value != null)
            {
                out_.Append(this.Value.String());
            }

            out_.Append(";");

            return out_.ToString();
        }
    }

    class ReturnStatement : Statement
    {
        public token.Token Token; // the 'return' token
        public Expression ReturnValue;

        public void statementNode() { }
        public string TokenLiteral() { return this.Token.Literal; }
        public string String()
        {
            System.Text.StringBuilder out_ = new System.Text.StringBuilder();

            out_.Append(this.TokenLiteral() + " ");

            if (this.ReturnValue != null)
            {
                out_.Append(this.ReturnValue.String());
            }

            out_.Append(";");

            return out_.ToString();
        }
    }

    class ExpressionStatement : Statement
    {
        public token.Token Token; // the first token of the expression
        public Expression Expression;

        public void statementNode() { }
        public string TokenLiteral() { return this.Token.Literal; }
        public string String()
        {
            if (this.Expression != null)
            {
                return this.Expression.String();
            }

            return "";
        }
    }
    
    class BlockStatement : Statement
    {
        public token.Token Token;
        public List<Statement> Statements;

        public void statementNode() { }
        public string TokenLiteral() { return this.Token.Literal; }
        public string String()
        {
            System.Text.StringBuilder out_ = new System.Text.StringBuilder();

            foreach (Statement s in Statements)
            {
                out_.Append(s.String());
            }

            return out_.ToString();
        }
    }


    // Expressions
    class Identifier : Expression
    {
        public token.Token Token; // the token.IDENT token
        public string Value;

        public void expressionNode() { }
        public string TokenLiteral() { return this.Token.Literal; }
        public string String() { return this.Value; }
    }

    struct Boolean : Expression
    {
        public token.Token Token;
        public bool Value;

        public void expressionNode() { }
        public string TokenLiteral() { return this.Token.Literal; }
        public string String() { return this.Token.Literal; }
    }

    struct IntegerLiteral : Expression
    {
        public token.Token Token;
        public long Value;

        public void expressionNode() { }
        public string TokenLiteral() { return this.Token.Literal; }
        public string String() { return this.Token.Literal; }
    }

    struct PrefixExpression : Expression
    {
        public token.Token Token; // The prefix token, e.g. !
        public string Operator;
        public Expression Right;

        public void expressionNode() { }
        public string TokenLiteral() { return this.Token.Literal; }
        public string String()
        {
            System.Text.StringBuilder out_ = new System.Text.StringBuilder();

            out_.Append("(");
            out_.Append(this.Operator);
            out_.Append(this.Right.String());
            out_.Append(")");

            return out_.ToString();
        }
    }

    struct InfixExpression : Expression
    {
        public token.Token Token; // The operator token, e.g. +
        public Expression Left;
        public string Operator;
        public Expression Right;

        public void expressionNode() { }
        public string TokenLiteral() { return this.Token.Literal; }
        public string String()
        {
            System.Text.StringBuilder out_ = new System.Text.StringBuilder();

            out_.Append("(");
            out_.Append(this.Left.String());
            out_.Append(" " + this.Operator + " ");
            out_.Append(this.Right.String());
            out_.Append(")");

            return out_.ToString();
        }
    }

    struct IfExpression : Expression
    {
        public token.Token Token; // The 'if' token
        public Expression Condition;
        public BlockStatement Consequence;
        public BlockStatement Alternative;

        public void expressionNode() { }
        public string TokenLiteral() { return this.Token.Literal; }
        public string String()
        {
            System.Text.StringBuilder out_ = new System.Text.StringBuilder();

            out_.Append("if");
            out_.Append(this.Condition.String());
            out_.Append(" ");
            out_.Append(this.Consequence.String());

            if (this.Alternative != null)
            {
                out_.Append("else ");
                out_.Append(this.Alternative.String());
            }

            return out_.ToString();
        }
    }

    struct FunctionLiteral : Expression
    {
        public token.Token Token; // The 'fn' token
        public List<Identifier> Parameters;
        public BlockStatement Body;
        public string Name;

        public void expressionNode() { }
        public string TokenLiteral() { return this.Token.Literal; }
        public string String()
        {
            System.Text.StringBuilder out_ = new System.Text.StringBuilder();

            List<string> params_ = new List<string>();
            foreach (Identifier p in this.Parameters)
            {
                params_.Add(p.String());
            }

            out_.Append(this.TokenLiteral());
            if (this.Name != null && this.Name != "")
            {
                out_.Append(string.Format("<{0}>", this.Name));
            }

            out_.Append("(");
            out_.Append(string.Join(", ", params_));
            out_.Append(") ");
            out_.Append(this.Body.String());

            return out_.ToString();
        }
    }

    struct CallExpression : Expression
    {
        public token.Token Token; // The '(' token
        public Expression Function;  // Identifier or FunctionLiteral
        public List<Expression> Arguments;

        public void expressionNode() { }
        public string TokenLiteral() { return this.Token.Literal; }
        public string String()
        {
            System.Text.StringBuilder out_ = new System.Text.StringBuilder();

            List<string> args = new List<string>();
            foreach (Expression a in this.Arguments)
            {
                args.Add(a.String());
            }

            out_.Append(this.Function.String());
            out_.Append("(");
            out_.Append(string.Join(", ", args));
            out_.Append(")");

            return out_.ToString();
        }
    }

    struct StringLiteral : Expression
    {
        public token.Token Token;
        public string Value;

        public void expressionNode() { }
        public string TokenLiteral() { return this.Token.Literal; }
        public string String() { return this.Token.Literal; }
    }

    struct ArrayLiteral : Expression
    {
        public token.Token Token; // the '[' token
        public List<Expression> Elements;

        public void expressionNode() { }
        public string TokenLiteral() { return this.Token.Literal; }
        public string String()
        {
            System.Text.StringBuilder out_ = new System.Text.StringBuilder();

            List<string> elements = new List<string>();
            foreach (Expression el in this.Elements)
            {
                elements.Add(el.String());
            }

            out_.Append("[");
            out_.Append(string.Join(", ", elements));
            out_.Append("]");

            return out_.ToString();
        }
    }

    struct IndexExpression : Expression
    {
        public token.Token Token; // The [ token
        public Expression Left;
        public Expression Index;

        public void expressionNode() { }
        public string TokenLiteral() { return this.Token.Literal; }
        public string String()
        {
            System.Text.StringBuilder out_ = new System.Text.StringBuilder();

            out_.Append("(");
            out_.Append(this.Left.String());
            out_.Append("[");
            out_.Append(this.Index.String());
            out_.Append("])");

            return out_.ToString();
        }
    }

    struct HashLiteral : Expression
    {
        public token.Token Token; // the '{' token
        public Dictionary<Expression, Expression> Pairs;

        public void expressionNode() { }
        public string TokenLiteral() { return this.Token.Literal; }
        public string String()
        {
            System.Text.StringBuilder out_ = new System.Text.StringBuilder();

            List<string> pairs = new List<string>();
            foreach (KeyValuePair<Expression, Expression> pair in this.Pairs)
            {
                pairs.Add(pair.Key.String() + ":" + pair.Value.String());
            }

            out_.Append("{");
            out_.Append(string.Join(", ", pairs));
            out_.Append("}");

            return out_.ToString();
        }
    }
}

