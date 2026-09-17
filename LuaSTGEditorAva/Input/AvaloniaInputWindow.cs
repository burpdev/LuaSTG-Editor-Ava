using Avalonia.Controls;
using Avalonia.Media;using Avalonia.Layout;using Avalonia.Interactivity;using Avalonia.Input;using Avalonia.Data;using Avalonia.Controls.Templates;using Avalonia.Controls.Primitives;using Avalonia;using System.Collections.Generic;
using LuaSTGEditorSharp.Windows.Input;
using System.ComponentModel;
using System.Threading.Tasks;
using System;
namespace LuaSTGEditorAva.Input
{
    public class AvaloniaInputWindow : Window, IAvaloniaInputWindow, INotifyPropertyChanged
    {
        protected static List<string> Separate(string s)
        {
            try
            {
                List<string> vs = new List<string>();
                int lastlocptr = 0;
                char[] c = s.ToCharArray();
                Stack<char> expr = new Stack<char>();
                for (int i = 0; i < c.Length; i++)
                {
                    if (c[i] == '(' || c[i] == '[' || c[i] == '{')
                    {
                        expr.Push(c[i]);
                    }
                    else if (c[i] == ')' || c[i] == ']' || c[i] == '}')
                    {
                        if (expr.Peek() == '(' && c[i] == ')') expr.Pop();
                        else if (expr.Peek() == '[' && c[i] == ']') expr.Pop();
                        else if (expr.Peek() == '{' && c[i] == '}') expr.Pop();
                        else throw new InvalidOperationException();
                    }
                    else if (c[i] == ',')
                    {
                        if (expr.Count == 0)
                        {
                            vs.Add(new string(c, lastlocptr, i - lastlocptr));
                            lastlocptr = i + 1;
                        }
                    }
                }
                vs.Add(new string(c, lastlocptr, c.Length - lastlocptr));
                return vs;
            }
            catch (InvalidOperationException)
            {
                return new List<string>() { s };
            }
            catch (NullReferenceException)
            {
                return new List<string>() { };
            }
        }

        protected static bool MatchFilter(string source, string filter)
        {
            if (string.IsNullOrEmpty(filter)) return true;
            return source != null && source.Contains(filter);
        }

        protected string result;

        public virtual string Result
        {
            get => result;
            set
            {
                result = value;
                RaisePropertyChanged("Result");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public void AppendTitle(string s)
        {
            Title = s + " - " + Title;
        }

        public virtual Task<bool?> ShowDialogAsync(Window owner)
        {
            return this.ShowDialog<bool?>(owner);
        }

        public bool? ShowDialog()
        {
            throw new NotSupportedException("Use ShowDialogAsync on Avalonia.");
        }

        protected void Accept()
        {
            Close(true);
        }

        protected void Cancel()
        {
            Close(false);
        }
    }
}
