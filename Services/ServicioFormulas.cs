using System.Globalization;

namespace CRM_ERP_UMG.Services
{
    public class ServicioFormulas
    {
        public decimal Evaluar(string expresion, Dictionary<string, decimal> variables)
        {
            var tokens = Tokenizar(expresion);
            var salidaPolaca = ConvertirAPolacaInversa(tokens);
            return EvaluarPolacaInversa(salidaPolaca, variables);
        }

        private List<string> Tokenizar(string expresion)
        {
            var tokens = new List<string>();
            int indice = 0;

            while (indice < expresion.Length)
            {
                char caracter = expresion[indice];

                if (char.IsWhiteSpace(caracter))
                {
                    indice++;
                    continue;
                }

                if (char.IsDigit(caracter) || caracter == '.' || caracter == ',')
                {
                    string numero = "";

                    while (indice < expresion.Length &&
                           (char.IsDigit(expresion[indice]) || expresion[indice] == '.' || expresion[indice] == ','))
                    {
                        numero += expresion[indice];
                        indice++;
                    }

                    tokens.Add(numero.Replace(",", "."));
                    continue;
                }

                if (char.IsLetter(caracter) || caracter == '_')
                {
                    string nombreVariable = "";

                    while (indice < expresion.Length &&
                           (char.IsLetterOrDigit(expresion[indice]) || expresion[indice] == '_'))
                    {
                        nombreVariable += expresion[indice];
                        indice++;
                    }

                    tokens.Add(nombreVariable);
                    continue;
                }

                if ("+-*/()".Contains(caracter))
                {
                    tokens.Add(caracter.ToString());
                    indice++;
                    continue;
                }

                throw new Exception($"Caracter no permitido en fórmula: {caracter}");
            }

            return tokens;
        }

        private List<string> ConvertirAPolacaInversa(List<string> tokens)
        {
            var salida = new List<string>();
            var operadores = new Stack<string>();

            string? tokenAnterior = null;

            foreach (var tokenOriginal in tokens)
            {
                var token = tokenOriginal;

                if (token == "-" && (tokenAnterior == null || EsOperador(tokenAnterior) || tokenAnterior == "("))
                {
                    salida.Add("0");
                }

                if (EsNumero(token) || EsVariable(token))
                {
                    salida.Add(token);
                }
                else if (EsOperador(token))
                {
                    while (operadores.Count > 0 &&
                           EsOperador(operadores.Peek()) &&
                           Prioridad(operadores.Peek()) >= Prioridad(token))
                    {
                        salida.Add(operadores.Pop());
                    }

                    operadores.Push(token);
                }
                else if (token == "(")
                {
                    operadores.Push(token);
                }
                else if (token == ")")
                {
                    while (operadores.Count > 0 && operadores.Peek() != "(")
                    {
                        salida.Add(operadores.Pop());
                    }

                    if (operadores.Count == 0)
                    {
                        throw new Exception("Paréntesis incorrectos.");
                    }

                    operadores.Pop();
                }

                tokenAnterior = tokenOriginal;
            }

            while (operadores.Count > 0)
            {
                var operador = operadores.Pop();

                if (operador == "(" || operador == ")")
                {
                    throw new Exception("Paréntesis incorrectos.");
                }

                salida.Add(operador);
            }

            return salida;
        }

        private decimal EvaluarPolacaInversa(List<string> tokens, Dictionary<string, decimal> variables)
        {
            var pila = new Stack<decimal>();

            foreach (var token in tokens)
            {
                if (EsNumero(token))
                {
                    pila.Push(decimal.Parse(token, CultureInfo.InvariantCulture));
                }
                else if (EsVariable(token))
                {
                    if (!variables.TryGetValue(token, out var valor))
                    {
                        throw new Exception($"Variable no encontrada: {token}");
                    }

                    pila.Push(valor);
                }
                else if (EsOperador(token))
                {
                    if (pila.Count < 2)
                    {
                        throw new Exception("Fórmula incompleta.");
                    }

                    var segundo = pila.Pop();
                    var primero = pila.Pop();

                    var resultado = token switch
                    {
                        "+" => primero + segundo,
                        "-" => primero - segundo,
                        "*" => primero * segundo,
                        "/" => segundo == 0 ? throw new DivideByZeroException() : primero / segundo,
                        _ => throw new Exception("Operador no soportado.")
                    };

                    pila.Push(resultado);
                }
            }

            if (pila.Count != 1)
            {
                throw new Exception("La fórmula no pudo evaluarse correctamente.");
            }

            return pila.Pop();
        }

        private bool EsNumero(string token)
        {
            return decimal.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
        }

        private bool EsVariable(string token)
        {
            return !string.IsNullOrWhiteSpace(token)
                   && (char.IsLetter(token[0]) || token[0] == '_')
                   && token.All(c => char.IsLetterOrDigit(c) || c == '_');
        }

        private bool EsOperador(string token)
        {
            return token is "+" or "-" or "*" or "/";
        }

        private int Prioridad(string operador)
        {
            return operador is "*" or "/" ? 2 : 1;
        }
    }
}
