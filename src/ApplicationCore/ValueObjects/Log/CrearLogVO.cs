//using ApplicationCore.ValueObjects.Roles;

namespace ApplicationCore.ValueObjects.Logs
{
    public class CrearLogVO
    {
        public int IdUsuario { get; set; }
        public int IdMetodo { get; set; }
        public string Parametros_Json { get; set; }
        public DateTime Created_On { get; set; }

        //public string Ip { get; set; }

    }
}