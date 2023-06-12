using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyNotes.Models;
public class Reminder
{
    public string? ReminderHeader { get; set; }
    public string? ReminderText { get; set; }
    public string? DateTime { get; set; }
    public string? Repeat { get; set; }
}

