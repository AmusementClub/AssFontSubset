using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AssFontSubset
{
    public class ProcessInfo : INotifyPropertyChanged
    {
        private Guid _taskId;
        private string _argument;
        private string _output;

        public Guid TaskId
        {
            get { return this._taskId; }
            set
            {
                if (value != this._taskId) {
                    this._taskId = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string Argument
        {
            get { return this._argument; }
            set
            {
                if (value != this._argument) {
                    this._argument = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string Output
        {
            get { return this._output; }
            set
            {
                if (value != this._output) {
                    this._output = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public override string ToString()
        {
            return this._argument;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
