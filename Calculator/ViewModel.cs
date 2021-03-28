using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.XPath;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace Calculator
{
    /// <summary>
    /// ViewModel for calculator
    /// </summary>
    [Preserve(AllMembers = true)]
    class ViewModel : BaseViewModel
    {
        #region Fields
        private Command buttonCommand;
        private string entry;
        private string formula;
        private string ans = "0";
        private string val = "0";
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance for the ViewModel class.
        /// </summary>
        public ViewModel()
        {
            this.ButtonCommand = new Command(ButtonClicked);
            this.ClearCommand = new Command(ClearClicked);
            this.BackspaceCommand = new Command(BackspaceClicked);
            this.CalculateCommand = new Command(CalculateClicked);
        }
        #endregion

        #region Command
        public Command ButtonCommand
        {
            get { return buttonCommand; }
            protected set { buttonCommand = value; }
        }
        public Command ClearCommand
        {
            get; set;
        }
        public Command BackspaceCommand
        {
            get; set;
        }
        public Command CalculateCommand
        {
            get; set;
        }

        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the property that bounds with the calculator entry.
        /// </summary>
        public string Entry
        {
            get { return this.entry; }
            set { this.entry = value; this.NotifyPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the property that bounds with the calculator answer entry.
        /// </summary>
        public string Answer
        {
            get { return this.ans; }
            set { this.ans = value; this.NotifyPropertyChanged(); }
        }
        #endregion

        #region Methods
        public void ClearClicked()
        {
            this.formula = "";
            UpdateEntry();
        }
        public void BackspaceClicked()
        {
            if (this.formula.Length > 0)
            {
                this.formula = this.formula.Remove(this.formula.Length - 1);
                UpdateEntry();
            }
        }
        public void ButtonClicked(object obj)
        {
            string str = obj as String;
            if (!String.IsNullOrEmpty(this.formula))
            {
                if (IsOperator(this.formula[this.formula.Length - 1]) && IsOperator(str[0]))
                    this.formula = this.formula.Remove(this.formula.Length - 1);
            }
            if (String.IsNullOrEmpty(this.formula) && (IsOperator(str[0]) || str[0] == '%'))
                return;
            this.formula += str[0];
            UpdateEntry();
        }

        public void CalculateClicked()
        {
            try { 
                val = Evaluate(this.formula).ToString();
                Answer = NumberCommas(val);
            }
            catch {
                Answer = "Error";
            }
        }

        public void UpdateEntry()
        {
            this.formula = this.formula.Replace("..", ".");
            this.formula = this.formula.Replace("%%", "%");
            Entry = FormulaFormat();
        }
        public bool IsOperator(char c)
        {
            if (c == '+' || c == '-' || c == 'X' || c == '÷') return true;
            return false;
        }
        public double Evaluate(string expression)
        {
            while(IsOperator(expression[expression.Length-1])) // helping the solver avoiding bad syntax ending
                expression = expression.Remove(expression.Length - 1);
 
            var xsltExpression =
                string.Format("number({0})",
                    new Regex(@"([\+\-])").Replace(expression, " ${1} ")
                                            .Replace("÷", " div ")
                                            .Replace("X", "*")
                                            .Replace("P", "(3.14159265359)")
                                            .Replace("A", "(" + val + ")")
                                            .Replace("%", " mod "));

            // ReSharper disable PossibleNullReferenceException
            return (double)new XPathDocument
                (new System.IO.StringReader("<r/>"))
                    .CreateNavigator()
                    .Evaluate(xsltExpression);
            // ReSharper restore PossibleNullReferenceException
        }

        private bool IsDigitsOnly(string s)
        {
            return s.All(c => (c >= '0' && c <= '9') || c == '.');
        }

        /// <summary>
        /// This function is responsible to return an number without commas to be with commas.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public string NumberCommas(object number)
        {
            if (!(number is int) && !(number is double) && !(number is string)) return "Syntax Error";
            string num;
            if (number is int || number is double) num = number.ToString();
            else
            {
                num = number as string;
                if (!IsDigitsOnly(num))
                    return num;
            }

            num = num.Trim();
            if (num.Length < 4) return num; // if less than 4 digits no need for commas

            string[] values = new string[2];
            if (num.IndexOf('.') != -1) // if decimal split it
            {
                values = num.Split('.');
                num = values[0];
                values[1] = "." + values[1];
            }

            string placeholder = "";
            int counter = 0;
            for (int i = num.Length - 1; i >= 0; i--)
            {
                counter++;
                if (counter == 4)
                {
                    placeholder += ",";
                    counter = 1;
                }
                placeholder += num[i];
            }

            num = "";
            for (int i = placeholder.Length - 1; i >= 0; i--) num += placeholder[i];
            placeholder = values[1]; // add decimal value
            return num + placeholder;
        }

        public string FormulaFormat()
        {
            if (String.IsNullOrEmpty(this.formula)) return "";
            int last = 0;
            string format = "";
            string holder = "";
            for(int i = 0; i < this.formula.Length; i++)
            {
                char c = this.formula[i];
                holder += c;
                if (IsOperator(c) || c == 'A' || c == '%' || c == 'P')
                {
                    format += NumberCommas(holder.Remove(holder.Length-1));
                    if (c == 'A')
                    {
                        if (!String.IsNullOrEmpty(format))
                            if (format[format.Length - 1] == ' ')
                                format = format.Remove(format.Length - 1);
                        format += " ANS ";
                    }
                    else if (c == 'P')
                    {
                        if (!String.IsNullOrEmpty(format))
                            if (format[format.Length - 1] == ' ')
                                format = format.Remove(format.Length - 1);
                        format += " π ";
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(format))
                            if (format[format.Length - 1] == ' ')
                                format = format.Remove(format.Length - 1);
                        format += " " + c + " ";
                    }
                    holder = "";
                }
            }
            format += NumberCommas(holder);
            return format;
        }
        #endregion
    }
}
