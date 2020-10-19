using System.Windows;
using System.Windows.Controls;

namespace TeamCityViewer
{
    public class MyComboBox : ComboBox
    {
        protected override Size MeasureOverride(Size constraint)
        {
            return base.MeasureOverride(new Size(MinWidth, constraint.Height));
        }
    }
}