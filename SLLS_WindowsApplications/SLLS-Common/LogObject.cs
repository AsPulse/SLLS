using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace SLLS_Common {

    public enum LOG_SEVERITY {
        INFO,
        WARN,
        ERROR,
    }

    public class LogObject {
        public string Content { get; set; } = "";
        public LOG_SEVERITY Severity { get; set; } = LOG_SEVERITY.INFO;
    }
    
    public class Logger {
        public ObservableCollection<LogObject> source = new();

        public void Log(LogObject log) {
            Application.Current.Dispatcher.Invoke(() => {
                source.Insert(0, log);
            });
        }
        public void Info(string content) {
            Log(new LogObject { Content = content, Severity = LOG_SEVERITY.INFO });
        }
        public void Warn(string content) {
            Log(new LogObject { Content = content, Severity = LOG_SEVERITY.WARN });
        }
        public void Error(string content) {
            Log(new LogObject { Content = content, Severity = LOG_SEVERITY.ERROR });
        }
    }

    [ValueConversion(typeof(LOG_SEVERITY), typeof(SolidColorBrush))]
    public class LogObjectConverter : IValueConverter {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if(value is LOG_SEVERITY s) {
                return new SolidColorBrush(
                    s switch {
                        LOG_SEVERITY.ERROR => Color.FromRgb(0xf7, 0xad, 0xbe),
                        LOG_SEVERITY.WARN => Color.FromRgb(0xf7, 0xe4, 0xad),
                        _ => Colors.Transparent,
                    }
                );
            }
            throw new NotImplementedException();
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

}
