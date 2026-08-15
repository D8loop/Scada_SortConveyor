using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Scada_SortConveyor.Data
{
    public class DashboardModel
    {
        // ObservableValue notifie LiveCharts automatiquement quand .Value change
        public ObservableValue ValCourtes { get; } = new(0);
        public ObservableValue ValLongues { get; } = new(0);
        public ObservableValue ValRejets { get; } = new(0);

        public ObservableValue ValAcceptees { get; } = new(0);
        public ObservableValue ValRejetees { get; } = new(0);

        public ISeries[] PiecesSeries { get; set; }
        public ISeries[] CadenceSeries { get; set; }

        public DashboardModel()
        {
            PiecesSeries = new ISeries[]
            {
                new PieSeries<ObservableValue>
                {
                    Values = new[] { ValCourtes },
                    Name = "Courtes",
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsSize = 14,
                    Fill = new SolidColorPaint(SKColors.DodgerBlue)
                },
                new PieSeries<ObservableValue>
                {
                    Values = new[] { ValLongues },
                    Name = "Longues",
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsSize = 14,
                    Fill = new SolidColorPaint(SKColors.Crimson)
                },
                new PieSeries<ObservableValue>
                {
                    Values = new[] { ValRejets },
                    Name = "Rejets",
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsSize = 14,
                    Fill = new SolidColorPaint(SKColors.Gray)
                }
            };

            CadenceSeries = new ISeries[]
            {
                new RowSeries<ObservableValue>
                {
                    Values = new[] { ValAcceptees },
                    Name = "Pièces Acceptées",
                    MaxBarWidth = 50,
                    Fill = new SolidColorPaint(SKColors.LimeGreen)
                },
                new RowSeries<ObservableValue>
                {
                    Values = new[] { ValRejetees },
                    Name = "Pièces Rejetées",
                    MaxBarWidth = 50,
                    Fill = new SolidColorPaint(SKColors.OrangeRed)
                }
            };
        }
    }
}