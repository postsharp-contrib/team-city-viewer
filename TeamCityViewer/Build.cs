using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace TeamCityViewer
{
    public class Build : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string State { get; set; }
        public int PercentageComplete { get; set; }
        public string BranchName { get; set; }
        public string StatusText { get; set; }
        public string BuildTypeName { get; set; }
        public DateTime QueuedDate { get; set; }
        public string DisplayedQueuedDate => QueuedDate.ToLocalTime().ToString("d. M. yyyy HH:mm:ss");
        public string TriggeredByName { get; set; }
        public string TriggeredByEmail { get; set; }

        public bool IsFinished => State == "finished";
        public bool IsQueued => State == "queued";
        public bool IsRunning => State == "running";

        public bool IsSuccess => Status == "SUCCESS";
        public bool IsFailure => Status == "FAILURE";
        public bool IsUnknown => Status == "UNKNOWN";

        public event PropertyChangedEventHandler PropertyChanged;

        public void RefreshOpacity(DateTime deempUpTo)
        {
            if (this.QueuedDate <= deempUpTo)
            {
                BuildOpacity = 0.3f;
            }
            else
            {
                BuildOpacity = 1;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BuildOpacity)));
        }

        public float BuildOpacity { get; private set;  }

        public Visibility ProgressBarVisibility
        {
            get
            {
                if (IsQueued)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

        public Brush DisplayedBackground
        {
            get
            {
                if (IsFinished)
                {
                    if (IsSuccess)
                    {
                        return Brushes.LightGreen;
                    }
                    else if (IsFailure)
                    {
                        return Brushes.Pink;
                    }
                    else if (IsUnknown)
                    {
                        return Brushes.LightGray;
                    }
                }
                else if (IsQueued)
                {
                    return Brushes.AntiqueWhite;
                }
                else if (IsRunning)
                {
                    return Brushes.LightYellow;
                }

                return Brushes.Brown;
            }
        }

        public Brush DisplayedForeground
        {
            get
            {
                if (IsFinished)
                {
                    if (IsSuccess)
                    {
                        return Brushes.LawnGreen;
                    }
                    else if (IsFailure)
                    {
                        return Brushes.LightPink;
                    }
                    else if (IsUnknown)
                    {
                        return Brushes.Silver;
                    }
                }
                else if (IsQueued)
                {
                    return Brushes.Transparent;
                }
                else if (IsRunning)
                {
                    return Brushes.Orange;
                }
                return Brushes.Brown;
            }
        }

        public int DisplayedPercentage
        {
            get
            {
                if (IsFinished)
                {
                    return 100;
                }
                else if (IsQueued)
                {
                    return 0;
                }
                else if (IsRunning)
                {
                    return PercentageComplete;
                }
                return PercentageComplete;
            }
        }

        public string BuildTypeId { get; internal set; }
    }
}