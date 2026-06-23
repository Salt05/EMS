using EMS.Core.Entities;

namespace EMS.Mvc.ViewModels;

public class MyEventViewModel
{
    public Registration Registration { get; set; } = null!;
    public Event Event { get; set; } = null!;
}
