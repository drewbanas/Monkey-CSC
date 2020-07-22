namespace lexer
{
    using token;

    using TokenType = System.String;

    struct Lexer
    {
        public string input;
        public int position; // current position in input (points to current char)
        public int readPosition; // current reading position in input (after current char)
        public char ch; // current char under examination

        public static Lexer New(string input)
        {
            Lexer l = new Lexer { input = input };
            l.readChar();
            return l;
        }

        public token.Token NextToken()
        {
            token.Token tok = new token.Token();

            this.skipWhitespace();

            switch (this.ch)
            {
                case '=':
                    if (this.peekChar() == '=')
                    {
                        char ch = this.ch;
                        this.readChar();
                        string literal = ch.ToString() + this.ch.ToString();
                        tok = new token.Token { Type = token.EQ, Literal = literal };
                    }
                    else
                    {
                        tok = newToken(token.ASSIGN, this.ch);
                    }
                    break;
                case '+':
                    tok = newToken(token.PLUS, this.ch);
                    break;
                case '-':
                    tok = newToken(token.MINUS, this.ch);
                    break;
                case '!':
                    if (this.peekChar() == '=')
                    {
                        char ch = this.ch;
                        this.readChar();
                        string literal = ch.ToString() + this.ch.ToString();
                        tok = new token.Token { Type = token.NOT_EQ, Literal = literal };
                    }
                    else
                    {
                        tok = newToken(token.BANG, this.ch);
                    }
                    break;
                case '/':
                    tok = newToken(token.SLASH, this.ch);
                    break;
                case '*':
                    tok = newToken(token.ASTERISK, this.ch);
                    break;
                case '<':
                    tok = newToken(token.LT, this.ch);
                    break;
                case '>':
                    tok = newToken(token.GT, this.ch);
                    break;
                case ';':
                    tok = newToken(token.SEMICOLON, this.ch);
                    break;
                case ':':
                    tok = newToken(token.COLON, this.ch);
                    break;
                case ',':
                    tok = newToken(token.COMMA, this.ch);
                    break;
                case '{':
                    tok = newToken(token.LBRACE, this.ch);
                    break;
                case '}':
                    tok = newToken(token.RBRACE, this.ch);
                    break;
                case '(':
                    tok = newToken(token.LPAREN, this.ch);
                    break;
                case ')':
                    tok = newToken(token.RPAREN, this.ch);
                    break;
                case '"':
                    tok.Type = token.STRING;
                    tok.Literal = this.readString();
                    break;
                case '[':
                    tok = newToken(token.LBRACKET, this.ch);
                    break;
                case ']':
                    tok = newToken(token.RBRACKET, this.ch);
                    break;
                case '\0':
                    tok.Literal = "";
                    tok.Type = token.EOF;
                    break;
                default:
                    if (isLetter(this.ch))
                    {

                        tok.Literal = this.readIdentifier();
                        tok.Type = token.LookupIdent(tok.Literal);
                        return tok;
                    }
                    else if (isDigit(this.ch))
                    {
                        tok.Type = token.INT;
                        tok.Literal = this.readNumber();
                        return tok;
                    }
                    else
                    {
                        tok = newToken(token.ILLEGAL, this.ch);
                    }
                    break;
            }

            this.readChar();
            return tok;
        }

        void skipWhitespace()
        {
            while (this.ch == ' ' || this.ch == '\t' || this.ch == '\n' || this.ch == '\r')
            {
                this.readChar();
            }
        }

        void readChar()
        {
            if (this.readPosition >= input.Length)
            {
                this.ch = '\0';
            }
            else
            {
                this.ch = this.input[this.readPosition];
            }
            this.position = this.readPosition;
            this.readPosition += 1;
        }

        char peekChar()
        {
            if (this.position >= this.input.Length)
            {
                return '\0';
            }
            else
            {
                return this.input[this.readPosition];
            }
        }

        string readIdentifier()
        {
            int _positon = this.position;
            while (isLetter(this.ch))
            {
                this.readChar();
            }

            return this.input.Substring(_positon, this.position - _positon);
        }

        string readNumber()
        {
            int _positon = this.position;
            while (isDigit(this.ch))
            {
                this.readChar();
            }

            return this.input.Substring(_positon, this.position - _positon);
        }

        string readString()
        {
            int _positon = this.position + 1;
            for (;;)
            {
                this.readChar();
                if (this.ch == '"' || this.ch == '\0')
                {
                    break;
                }
            }

            return this.input.Substring(_positon, this.position - _positon);
        }

        static bool isLetter(char ch)
        {
            return 'a' <= ch && ch <= 'z' || 'A' <= ch && ch <= 'Z' || ch == '_';
        }

        static bool isDigit(char ch)
        {
            return '0' <= ch && ch <= '9';
        }

        static token.Token newToken(TokenType tokenType, char ch)
        {
            return new token.Token { Type = tokenType, Literal = ch.ToString() };
        }
    }
}
