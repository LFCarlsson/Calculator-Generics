using System;
using System.Collections.Generic;
using System.Globalization;


namespace Calculator_Generics
{
    class GenericCalculator
    {
        /*Holds the precedence of operators. Higher number = higher precedence. Brackets are given a negative precedence 
         * make the shunting yard algorithm a little bit simpler to implement.
         * 
         * TODO: if right assosiative operators are added, this might be where the assossiativity should be stored to.
         *       alternatively a second dictionary could hold it.
        */
        static private Dictionary<string, int> operatorPrecedence = new Dictionary<string, int>
        {
            {"+",0},
            {"-",0},
            {"*",1},
            {"/",1},
            {"(",-1},
            {")",-1}
        };

        //Create a delegate type and use a dictionary to store strings and corresponding delegates
        delegate void OperatorDelegate();
        static private Dictionary<string, OperatorDelegate> operatorDelegates;

        static void Main(string[] args)
        {
            Run<int>();
            Run<double>();
            Run<decimal>();
            Console.Read();
        }

        static public void Run<T>()
        {
            Init<T>();
            while (true)
            {
                Console.WriteLine(typeof(T).Name + " mode. Enter expression or 'quit':");
                var expression = CheckAndClean(Console.ReadLine());
                if (expression == "quit")
                {
                    break;
                }
                var tokens = Tokenize<T>(expression);
                ShuntingYard<T>(tokens);
                T ans = Evaluate<T>();
                Console.WriteLine(ans);
            }
            Console.WriteLine("Goodbye!");

        }



        static private void AddPush<T>()
        {
            dynamic y = (T) stack.Pop();
            dynamic x = (T) stack.Pop();
            dynamic z = x + y;
            stack.Push((T)z);
        }

        static private void SubPush<T>()
        {
            dynamic y = (T) stack.Pop();
            dynamic x = (T) stack.Pop();
            dynamic z = x - y;

            stack.Push((T)z);
        }

        static private void MultPush<T>()
        {
            dynamic y = (T)stack.Pop();
            dynamic x = (T)stack.Pop();
            dynamic z = x * y;

            stack.Push((T)z);
        }
        ///<summary>
        ///Pops operands, perform divison and pushes the result back to the stack
        ///</summary>
        ///<exception cref="DivideByZeroException"></exception>
        static private void DivPush<T>()
        {
            dynamic y = (T)stack.Pop();
            dynamic x = (T)stack.Pop();
            if(y == 0)
            {
                throw (new DivideByZeroException("DivPush: Attempted division by zero") );
            }
            dynamic z = x / y;

            stack.Push((T)z);
        }
        static private Queue<object> outputQueue;
        static private Stack<object> stack;
        static private char decimalSeperator;


        static private T Evaluate<T>()
        {
            stack.Clear();
            while (outputQueue.Count != 0)
            {
                object token = outputQueue.Dequeue();



                //Values get pushed onto the stack
                
                if (token.GetType() == typeof(T))
                {
                    stack.Push(token);
                }
                //if not a value the token is an operator. 
                // pop operands perform operation and push result to stack.
                else
                {
                    //look up the correct delegate method in the dictionary and run it.
                    operatorDelegates[(string)token]();
                }

            }

            //After performing all operations the result will be in the stack.
            return (T)stack.Pop();
        }




        static public void Init<T>()
        {
            outputQueue = new Queue<object>();
            stack = new Stack<object>();
            decimalSeperator = Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            operatorDelegates = new Dictionary<string, OperatorDelegate>
        {
            {"+",AddPush<T>},
            {"-",SubPush<T>},
            {"*",MultPush<T>},
            {"/",DivPush<T>}
        };
        }


        static private string CheckAndClean(string expression)
        {
            string result = expression.Replace(" ", "");
            //TODO: assert only computable expression
            return result;
        }

        static private bool IsNegationHelper(char last)
        {
            //first character being '-' means it's part of negative operand
            if (last == '?')
            {
                return true;
            }
            // if the last character was an operator '-' is part of negative operand. ')' needs to be handled
            // since brackets are in the operator list.
            if (last != ')' && operatorPrecedence.ContainsKey(last.ToString()))
            {
                return true;
            }
            return false;
        }

        static private Queue<object> Tokenize<T>(string inputLine)
        {

            Queue<object> tokens = new Queue<object>();
            string valueBuffer = "";
            char lastChar = '?'; //random char not used later in code to represent that we are on first char.
            T operand = default(T);
            Type Ttype = typeof(T);
            var parseMethod = Ttype.GetMethod("Parse", new Type[] {typeof(string) });

            for (int i = 0; i < inputLine.Length; i++)
            {
                char c = inputLine[i];

                //Since we don't want to force the user to use spaces to separate tokens we need to buffer up digits to for value tokens.
                //We know that we have gotten all the digits when we find an operator or the end of the expression.

                if (Char.IsDigit(c) || c == decimalSeperator)
                {
                    valueBuffer += c.ToString();

                    //special handling to make operands of type '.1' etc work.
                    if (valueBuffer[0] == decimalSeperator)
                    {
                        valueBuffer = '0' + valueBuffer;
                    }

                    //Parsing the string into a type T value. All reasonable types will have the Parse method.
                    //TODO: exception handling

                    operand = (T) parseMethod.Invoke(null,new object[] { valueBuffer });
                }
                //end of an operand token.
                else
                {

                    //if we have buffered up an operand enqueue it
                    if (valueBuffer != "")
                    {
                        tokens.Enqueue(operand);
                        valueBuffer = "";
                    }
                    //one of the cases where there isn't anything in the buffer is negations. Push in a 0 to make life easier
                    //TODO: doesn't handle expressions of type x * -y correctly. Expands to x * 0 - y = 0.. Kind of tricky to solve, especially for expressions like x * - (y .. )
                    //Possible solution: insert brackets, tricky in the second case, but should be doable.
                    else
                    {
                        if (c == '-' && IsNegationHelper(lastChar))
                        {
                            tokens.Enqueue( (T)parseMethod.Invoke(null, new object[] { "0" }) );
                        }
                    }

                    tokens.Enqueue(c.ToString());
                }
                lastChar = c;
            }
            //After last char is processed there may be a value left in the value buffer not yet enqueued
            if (valueBuffer != "")
            {
                tokens.Enqueue((T)parseMethod.Invoke(null, new object[] { valueBuffer }));
            }
            
            return tokens;
        }

        static private void ShuntingYard<T>(Queue<object> tokens)
        {
            outputQueue.Clear();
            stack.Clear();
            object token;
            Type numType = typeof(T);
            while (tokens.Count > 0)
            {
                token = tokens.Dequeue();
                bool wasValue = token.GetType() == numType;
                if(wasValue)
                {
                    outputQueue.Enqueue(token);
                }

                //Handle brackets TODO: more comments.
                else if ((string)token == "(")
                {
                    stack.Push(token);
                }
                else if ((string)token == ")")
                {
                    while (stack.Count != 0 && (string)stack.Peek() != "(")
                    {
                        outputQueue.Enqueue(stack.Pop());
                    }
                    if (stack.Count == 0)
                    {
                        Console.WriteLine("Unbalanced brackets!");

                    }
                    //remove the '(' that should be on top of the stack
                    stack.Pop();
                }
                else
                {

                    //if operator on top of operator stack is of equal or higher precedence pop and enque it on output queue.
                    // repeat until lower precedence on top of stack or empty stack. Then push the incoming operator onto stack.
                    while (stack.Count != 0
                            && operatorPrecedence[(string)stack.Peek()] >= operatorPrecedence[(string)token]
                           )
                    {
                        outputQueue.Enqueue(stack.Pop());
                    }
                    stack.Push(token);
                }

            }
            //Pop the content of the operator stack onto the output queue
            while (stack.Count > 0)
            {
                outputQueue.Enqueue(stack.Pop());
            }
        }
    }
}