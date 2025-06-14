using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace QuizClient.Service
{
    public class EqualityConverter : IValueConverter
    {
        // Convierte: si el valor actual (OpcionSeleccionada) es igual al parámetro (RadioButton), devuelve true.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == parameter?.ToString();
        }

        // Convierte de vuelta: si el RadioButton se selecciona, devuelve su valor como nueva opción seleccionada.
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return parameter;
            return Binding.DoNothing;
        }
    }
}
