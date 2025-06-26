using GrainInterfaces;

namespace Grains
{
    public class AppointmentGrain : Grain, IAppointmentGrain
    {

        private string _title = string.Empty;
        private string _description = string.Empty;
        private DateTime _date;

        public Task SetDetails(string title, DateTime date, string description)
        {
            this._title = title;
            this._date = date;
            this._description = description;
            return Task.CompletedTask;
        }

        public Task<(string Title, DateTime Date, string Description)> GetDetails()
        {
         return Task.FromResult((_title, _date,_description ));

        }

        
    }
}