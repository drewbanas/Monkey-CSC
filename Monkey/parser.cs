namespace parser
{
    using System.Collections.Generic;

    using lexer;
    using token;
    using TokenType = System.String;

    struct Parser
    {
        const int LOWEST = 1;
        const int EQUALS = 2;      // ==
        const int LESSGREATER = 3; // > or <
        const int SUM = 4;         // +
        const int PRODUCT = 5;     // *
        const int PREFIX = 6;      // -X or !X
        const int CALL = 7;        // myFunction(X)
        const int INDEX = 8;       // array[index]

        static Dictionary<TokenType, int> precedecnces =
                new System.Collections.Generic.Dictionary<TokenType, int>
                {
                    {token.EQ, EQUALS},
                    {token.NOT_EQ, EQUALS},
                    {token.LT, LESSGREATER},
                    {token.GT, LESSGREATER},
                    {token.PLUS, SUM},
                    {token.MINUS, SUM},
                    {token.SLASH, PRODUCT},
                    {token.ASTERISK, PRODUCT},
                    {token.LPAREN, CALL},
                    {token.LBRACKET, INDEX}
                };

        delegate ast.Expression prefixParseFn();
        delegate ast.Expression infixParseFn(ast.Expression left);

        static Lexer l;
        static List<string> errors;

        static token.Token curToken;
        static token.Token peekToken;

        static Dictionary<TokenType, prefixParseFn> prefixParseFns;
        static Dictionary<TokenType, infixParseFn> infixParseFns;

        public static Parser New(Lexer l)
        {
            Parser p = new Parser();//
            Parser.l = l;
            Parser.errors = new List<string>();
 
            prefixParseFns = new Dictionary<TokenType, prefixParseFn>();
            p.registerPrefix(token.IDENT, parseIdentifier);
            p.registerPrefix(token.INT, parseIntegerLiteral);
            p.registerPrefix(token.STRING, parseStringLiteral);
            p.registerPrefix(token.BANG, parsePrefixExpression);
            p.registerPrefix(token.MINUS, parsePrefixExpression);
            p.registerPrefix(token.TRUE, parseBoolean);
            p.registerPrefix(token.FALSE, parseBoolean);
            p.registerPrefix(token.LPAREN, parseGroupedExpression);
            p.registerPrefix(token.IF, parseIfExpression);
            p.registerPrefix(token.FUNCTION, parseFunctionLiteral);
            p.registerPrefix(token.LBRACKET, parseArrayLiteral);
            p.registerPrefix(token.LBRACE, parseHashLiteral);

            infixParseFns = new Dictionary<TokenType, infixParseFn>();
            p.registerInfix(token.PLUS, parseInfixExpression);
            p.registerInfix(token.MINUS, parseInfixExpression);
            p.registerInfix(token.SLASH, parseInfixExpression);
            p.registerInfix(token.ASTERISK, parseInfixExpression);
            p.registerInfix(token.EQ, parseInfixExpression);
            p.registerInfix(token.NOT_EQ, parseInfixExpression);
            p.registerInfix(token.LT, parseInfixExpression);
            p.registerInfix(token.GT, parseInfixExpression);

            p.registerInfix(token.LPAREN, parseCallExpression);
            p.registerInfix(token.LBRACKET, parseIndexExpression);

            // Read two tokens, so curToken and peekToken are both set
            nextToken();
            nextToken();

            return p;
        }

        static void nextToken()
        {
            curToken = peekToken;
            peekToken = l.NextToken();
        }

        static bool curTokenIs(TokenType t)
        {
            return curToken.Type == t;
        }

        static bool peekTokenIs(TokenType t)
        {
            return peekToken.Type == t;
        }

        static bool expectPeek(TokenType t)
        {
            if (peekTokenIs(t))
            {
                nextToken();
                return true;
            }
            else
            {
                peekError(t);
                return false;
            }
        }

        public static List<string> Errors()
        {
            return errors;
        }

        static void peekError(TokenType t)
        {
            string msg = string.Format("expected next token to be {0}, got {1} instead", t, peekToken.Type);
            errors.Add(msg);
        }

        static void noPrefixFnError(TokenType t)
        {
            string msg = string.Format("no prefix function for {0} found", t);
            errors.Add(msg);
        }

        public ast.Program ParseProgram()
        {
            ast.Program program = new ast.Program { };
            program.Statements = new List<ast.Statement>();

            while (!curTokenIs(token.EOF))
            {
                ast.Statement stmt = parseStatement();
                if (stmt != null)
                {
                    program.Statements.Add(stmt);
                }
                nextToken();
            }

            return program;
        }

        static ast.Statement parseStatement()
        {
            switch (curToken.Type)
            {
                case token.LET:
                    return parseLetStatement();
                case token.RETURN:
                    return parseReturnStatement();
                default:
                    return parseExpressionStatement();
            }
        }

        static ast.LetStatement parseLetStatement()
        {

            ast.LetStatement stmt = new ast.LetStatement { Token = curToken };

            if (!expectPeek(token.IDENT))
            {
                return null;
            }

            stmt.Name = new ast.Identifier { Token = curToken, Value = curToken.Literal };

            if (!expectPeek(token.ASSIGN))
            {
                return null;
            }

            nextToken();

            stmt.Value = parseExpression(LOWEST);

            if(stmt.Value is ast.FunctionLiteral)
            {
                ast.FunctionLiteral fl = (ast.FunctionLiteral)stmt.Value;
                fl.Name = stmt.Name.Value;
                stmt.Value = fl;
            }

            if (peekTokenIs(token.SEMICOLON))
            {
                nextToken();
            }

            return stmt;
        }

        static ast.ReturnStatement parseReturnStatement()
        {
            ast.ReturnStatement stmt = new ast.ReturnStatement { Token = curToken };

            nextToken();

            stmt.ReturnValue = parseExpression(LOWEST);

            if (peekTokenIs(token.SEMICOLON))
            {
                nextToken();
            }

            return stmt;
        }

        static ast.ExpressionStatement parseExpressionStatement()
        {
            ast.ExpressionStatement stmt = new ast.ExpressionStatement { Token = curToken };

            stmt.Expression = parseExpression(LOWEST);

            if (peekTokenIs(token.SEMICOLON))
            {
                nextToken();
            }

            return stmt;
        }

        static ast.Expression parseExpression(int precedence)
        {
            prefixParseFn prefix;
            if (curToken.Type == null || !prefixParseFns.TryGetValue(curToken.Type, out prefix))
            {
                noPrefixFnError(curToken.Type);
                return null;
            }
            
            ast.Expression leftExp = prefix();

            while (!peekTokenIs(token.SEMICOLON) && precedence < peekPrecedence())
            {
                infixParseFn infix;
                if (peekToken.Type == null || !infixParseFns.TryGetValue(peekToken.Type, out infix))
                {
                    return leftExp;
                }

                nextToken();

                leftExp = infix(leftExp);
            }

            return leftExp;
        }

        static int peekPrecedence()
        {
            int p;    
            if (peekToken.Type != null && precedecnces.TryGetValue(peekToken.Type, out p))
            {
                return p;
            }

            return LOWEST;
        }

        static int curPrecedence()
        {
            int p;
            if (curToken.Type != null && precedecnces.TryGetValue(curToken.Type, out p))
            {
                return p;
            }

            return LOWEST;
        }

        static ast.Expression parseIdentifier()
        {
            return new ast.Identifier { Token = curToken, Value = curToken.Literal };
        }

        static ast.Expression parseIntegerLiteral()
        {
            ast.IntegerLiteral lit = new ast.IntegerLiteral { Token = curToken };

            long value;
            if (!long.TryParse(curToken.Literal, out value))
            {
                string msg = string.Format("could not parse {0} as integer", curToken.Literal);
                errors.Add(msg);
                return null;
            }

            lit.Value = value;

            return lit;
        }

        static ast.Expression parseStringLiteral()
        {
            return new ast.StringLiteral { Token = curToken, Value = curToken.Literal };
        }

        static ast.Expression parsePrefixExpression()
        {
            ast.PrefixExpression expression = new ast.PrefixExpression
            {
                Token = curToken,
                Operator = curToken.Literal
            };

            nextToken();

            expression.Right = parseExpression(PREFIX);

            return expression;
        }

        static ast.Expression parseInfixExpression(ast.Expression left)
        {
            ast.InfixExpression expression = new ast.InfixExpression
            {
                Token = curToken,
                Operator = curToken.Literal,
                Left = left
            };

            int precedence = curPrecedence();
            nextToken();
            expression.Right = parseExpression(precedence);

            return expression;
        }

        static ast.Expression parseBoolean()
        {
            return new ast.Boolean { Token = curToken, Value = curTokenIs(token.TRUE) };
        }

        static ast.Expression parseGroupedExpression()
        {
            nextToken();

            ast.Expression exp = parseExpression(LOWEST);

            if (!expectPeek(token.RPAREN))
            {
                return null;
            }

            return exp;
        }

        static ast.Expression parseIfExpression()
        {
            ast.IfExpression expression = new ast.IfExpression { Token = curToken };

            if (!expectPeek(token.LPAREN))
            {
                return null;
            }

            nextToken();
            expression.Condition = parseExpression(LOWEST);

            if (!expectPeek(token.RPAREN))
            {
                return null;
            }

            if (!expectPeek(token.LBRACE))
            {
                return null;
            }

            expression.Consequence = parseBlockStatement();

            if (peekTokenIs(token.ELSE))
            {
                nextToken();

                if (!expectPeek(token.LBRACE))
                {
                    return null;
                }

                expression.Alternative = parseBlockStatement();
            }

            return expression;
        }


        static ast.BlockStatement parseBlockStatement()
        {
            ast.BlockStatement block = new ast.BlockStatement { Token = curToken };
            block.Statements = new List<ast.Statement>();

            nextToken();

            while (!curTokenIs(token.RBRACE) && !curTokenIs(token.EOF))
            {
                ast.Statement stmt = parseStatement();
                if (stmt != null)
                {
                    block.Statements.Add(stmt);
                }
                nextToken();
            }

            return block;
        }

        static ast.Expression parseFunctionLiteral()
        {
            ast.FunctionLiteral lit = new ast.FunctionLiteral { Token = curToken };

            if (!expectPeek(token.LPAREN))
            {
                return null;
            }

            lit.Parameters = parseFunctionParameters();

            if (!expectPeek(token.LBRACE))
            {
                return null;
            }

            lit.Body = parseBlockStatement();

            return lit;
        }

        static List<ast.Identifier> parseFunctionParameters()
        {
            List<ast.Identifier> identifiers = new List<ast.Identifier>();

            if (peekTokenIs(token.RPAREN))
            {
                nextToken();
                return identifiers;
            }

            nextToken();

            ast.Identifier ident = new ast.Identifier { Token = curToken, Value = curToken.Literal };
            identifiers.Add(ident);

            while (peekTokenIs(token.COMMA))
            {
                nextToken();
                nextToken();
                ident = new ast.Identifier { Token = curToken, Value = curToken.Literal };
                identifiers.Add(ident);
            }

            if (!expectPeek(token.RPAREN))
            {
                return null;
            }

            return identifiers;
        }

        static ast.Expression parseCallExpression(ast.Expression function)
        {
            ast.CallExpression exp = new ast.CallExpression { Token = curToken, Function = function };
            exp.Arguments = parseExpressionList(token.RPAREN);
            return exp;
        }

        static List<ast.Expression> parseExpressionList(TokenType end)
        {
            List<ast.Expression> list = new List<ast.Expression>();
            if (peekTokenIs(end))
            {
                nextToken();
                return list;
            }

            nextToken();
            list.Add(parseExpression(LOWEST));

            while (peekTokenIs(token.COMMA))
            {
                nextToken();
                nextToken();
                list.Add(parseExpression(LOWEST));
            }

            if (!expectPeek(end))
            {
                return null;
            }

            return list;
        }

        static ast.Expression parseArrayLiteral()
        {
            ast.ArrayLiteral array = new ast.ArrayLiteral { Token = curToken };

            array.Elements = parseExpressionList(token.RBRACKET);

            return array;
        }

        static ast.Expression parseIndexExpression(ast.Expression left)
        {
            ast.IndexExpression exp = new ast.IndexExpression { Token = curToken, Left = left };

            nextToken();
            exp.Index = parseExpression(LOWEST);

            if (!expectPeek(token.RBRACKET))
            {
                return null;
            }

            return exp;
        }

        static ast.Expression parseHashLiteral()
        {
            ast.HashLiteral hash = new ast.HashLiteral { Token = curToken };
            hash.Pairs = new Dictionary<ast.Expression, ast.Expression>();

            while (!peekTokenIs(token.RBRACE))
            {
                nextToken();
                ast.Expression key = parseExpression(LOWEST);

                if (!expectPeek(token.COLON))
                {
                    return null;
                }

                nextToken();
                ast.Expression value = parseExpression(LOWEST);

                hash.Pairs.Add(key, value);

                if (!peekTokenIs(token.RBRACE) && !expectPeek(token.COMMA))
                {
                    return null;
                }
            }

            if(!expectPeek(token.RBRACE))
            {
                return null;
            }

            return hash;
        }

        void registerPrefix(TokenType tokenType, prefixParseFn fn)
        {
            prefixParseFns.Add(tokenType, fn);
        }

        void registerInfix(TokenType tokenType, infixParseFn fn)
        {
            infixParseFns.Add(tokenType, fn);
        }

    }

}
