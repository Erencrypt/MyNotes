using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing.Printing;
using System.Xml.Linq;
using Windows.UI.Text;

namespace MyNotes.Models
{
    public class TaskModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}