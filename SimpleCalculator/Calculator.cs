using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SimpleCalculator
{
    class Calculator : DynamicObject
    {
        public CultureInfo CultureInfo { get; set; }
        private readonly Stack<Expression> expressions = new Stack<Expression>();
        private readonly Stack<Symbol> operators = new Stack<Symbol>();

        public Calculator() : this(CultureInfo.InvariantCulture)
        { 
        }

        private Calculator(CultureInfo cultureInfo)
        {
            CultureInfo = cultureInfo;
        }

        public decimal Result(string expression, object argument = null)
        {
            var arguments = ArgumentsParsing(argument);
            return Result(expression, arguments);
        }

        private decimal Result(string expression, Dictionary<string, decimal> arguments)
        {
            var compiled = Parsing(expression);
            return Execute(compiled, arguments);
        }

        private decimal Execute(Func<decimal[], decimal> compiled, Dictionary<string, decimal> arguments)
        {
            arguments = arguments ?? new Dictionary<string, decimal>();
            var values = arguments.Select(parameter => arguments[""]).ToArray();
            try { return compiled(values); }
            catch (DivideByZeroException)
            {
                Console.WriteLine("Нельзя делить на 0");
                return 0;
            }
        }

        private Dictionary<string, decimal> ArgumentsParsing(object argument)
        {
            if (argument == null)
            {
                return new Dictionary<string, decimal>();
            }

            var type = argument.GetType();

            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(predicates => predicates.CanRead && IsNumber(predicates.PropertyType));

            var arguments = properties.ToDictionary(property => property.Name,
                property => Convert.ToDecimal(property.GetValue(argument, null)));

            return arguments;
        }
                

        private Func<decimal[],decimal> Parsing(string expression)
        {
            expressions.Clear();
            operators.Clear();

            if (string.IsNullOrWhiteSpace(expression))
            {
                return res=>0;
            }            
            using (var stringReader = new StringReader(expression))
            {               
                int peek;                                
                while((peek = stringReader.Peek())>-1)
                {   
                    var next = (char)peek;                    

                    if (char.IsDigit(next))
                    {
                        expressions.Push(readOperand(stringReader));
                        continue;
                    }
                    if (Operation.IsDefined(next))
                    {
                        if(next=='-' && expressions.Count==0)
                        {
                            stringReader.Read();
                            operators.Push(Operation.Negate);
                            continue;
                        }

                        var currentOperand = readOperation(stringReader);
                        ComputeWhile(() => operators.Count > 0 && operators.Peek() != Brackets.Left &&
                            currentOperand.Order <= ((Operation)operators.Peek()).Order);

                        operators.Push(currentOperand);
                        continue;
                    }
                    if (next == '(' && ((operators.Count == 0 && expressions.Count == 0) |  operators.Count>0))
                    {
                        stringReader.Read();
                        operators.Push(Brackets.Left);
                        if (stringReader.Peek() == '-')
                        {
                            stringReader.Read();
                            operators.Push(Operation.Negate);
                        }
                       continue;                                                 
                    }

                    if (next == ')')
                    {
                        try 
                        {
                            stringReader.Read();
                            ComputeWhile(() => operators.Count > 0 && operators.Peek() != Brackets.Left);
                            operators.Pop();
                            continue;
                        }
                        catch (Exception) { Console.WriteLine("Отсутсвует открывающая скобка"); }
                    }
                    if(next==' ')
                    {
                        stringReader.Read();
                    }
                    if(next!=' ')
                    {
                        throw new ArgumentException(string.Format("Encountered invalid character {0}", next), "expression");
                    }
                    
                }
            }
           
                ComputeWhile(() => operators.Count > 0);

            var arrayParameter = Expression.Parameter(typeof(decimal[]), "args");
            var lambda = Expression.Lambda<Func<decimal[], decimal>>(expressions.Pop(), arrayParameter);
            var compiled = lambda.Compile();
            return compiled;
        }

        private void ComputeWhile(Func<bool> condition)
        {
            while (condition())
            {
                try
                {
                    var operand = (Operation)operators.Pop();
                    var strokes = new Expression[operand.OperandsNumber];
                    for (var i = operand.OperandsNumber - 1; i >= 0; i--)
                    {
                        strokes[i] = expressions.Pop();
                    }
                    expressions.Push(operand.Implement(strokes));
                }
                catch (InvalidCastException) { Console.WriteLine("Отсутсвует закрывающая скобка");}
            }
        }


        private Expression readOperand(TextReader textReader)
        {
            var decimalSeparator = CultureInfo.NumberFormat.NumberDecimalSeparator[0];
            var groupSeparator = CultureInfo.NumberFormat.NumberGroupSeparator[0];

            var operand = string.Empty;

            int peek;
            while ((peek = textReader.Peek()) > -1)
            {
                var next = (char)peek;

                if (char.IsDigit(next) || next == decimalSeparator || next == groupSeparator)
                {
                    textReader.Read();
                    operand += next;
                }
                else
                {
                    break;
                }
            }
            return Expression.Constant(decimal.Parse(operand, CultureInfo));
        }
        private Operation readOperation(TextReader textReader)
        {
            var operation = (char)textReader.Read();
            return (Operation)operation;
        }

        private bool IsNumber(Type propertyType)
        {
            switch (Type.GetTypeCode(propertyType))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
            }
            return false;
        }
    }

    internal sealed class Operation : Symbol
    {
        private readonly Func<Expression, Expression, Expression> binary_operation;
        private readonly Func<Expression, Expression> unary_operation;

        public string Name { get; private set; }
        public int Order { get; private set; }
        public int OperandsNumber{ get; private set; }

        public static readonly Operation Add = new Operation(1, Expression.Add, "Add");
        public static readonly Operation Subtract = new Operation(1, Expression.Subtract, "Subtract");
        public static readonly Operation Multiply = new Operation(2, Expression.Multiply, "Multiply");
        public static readonly Operation Divide = new Operation(2, Expression.Divide, "Divide");
        public static readonly Operation Negate = new Operation(2, Expression.Negate, "Negate");

        private static readonly Dictionary<char, Operation> operationsDictionary = new Dictionary<char, Operation>
        {
            {'+',Add },
            {'-',Subtract },
            {'*',Multiply },
            {'/',Divide }
        };

        private Operation(int order, string name)
        {
            Name = name;
            Order = order;
        }

        private Operation(int order, Func<Expression, Expression> unary_operation, string name): this(order, name)
        {
            this.unary_operation = unary_operation;
                OperandsNumber = 1;
        }

        private Operation(int order, Func<Expression,Expression, Expression> binary_operation, string name): this(order, name)
        {
            this.binary_operation = binary_operation;
                OperandsNumber = 2;
        }

        public static explicit operator Operation(char operand)
        {
            Operation result;
            if(operationsDictionary.TryGetValue(operand, out result))
            {
                return result;
            }
            else
            {
                throw new InvalidCastException();
            }
        }

        public static bool IsDefined(char operand)
        {
            return operationsDictionary.ContainsKey(operand);
        }

        private Expression Implement(Expression expression)
        {
            return unary_operation(expression);
        }
        private Expression Implement(Expression left, Expression right)
        {
            return binary_operation(left,right);
        }

        public Expression Implement(params Expression[] expressions)
        {
            if (expressions.Length == 1)
            {
                return Implement(expressions[0]);
            }

            if (expressions.Length == 2)
            {
                return Implement(expressions[0], expressions[1]);
            }

            throw new NotImplementedException();
        }
    }

    internal class Brackets : Symbol
    {
        public static readonly Brackets Left = new Brackets();
        public static readonly Brackets Right = new Brackets();

        private Brackets()
        {
        }
    }

    internal class Symbol
    {
    }
}
