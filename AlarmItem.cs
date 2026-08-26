using System.ComponentModel;

namespace FactorialApp
{
    public class AlarmItem : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Timestamp { get; set; }
        public string Message { get; set; }

        private bool _isAcknowledged;
        public bool IsAcknowledged
        {
            get { return _isAcknowledged; }
            set { _isAcknowledged = value; OnPropertyChanged(nameof(IsAcknowledged)); OnPropertyChanged(nameof(StatusText)); }
        }

        public string StatusText => IsAcknowledged ? "已确认" : "未确认";

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
    }
}