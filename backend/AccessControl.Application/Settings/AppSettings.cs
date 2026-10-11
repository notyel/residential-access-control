namespace AccessControl.Application.Settings
{
    public class AppSettings
    {
        public const string SectionName = "App";

        /// <summary>Dirección pública del frontend; con ella se arman los enlaces que se envían por correo.</summary>
        public string FrontendUrl { get; set; } = "http://localhost:4400";
    }
}
