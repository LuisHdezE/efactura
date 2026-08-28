using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.Utilities.Helpers
{
    namespace ApplicationCore.Common.Helpers
    {
        public static class StringHelper
        {
            public static string FormatearCampos(string campos)
            {
                var elementos = campos.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var resultado = string.Join(", ", elementos.Select(e => $"\"\"{e.Trim()}\"\""));
                return resultado;
            }

            // Obtiene las propiedades de la entidad
            public static List<System.Reflection.PropertyInfo> GetNonNullProperties<TEntity>(TEntity entity)
            {
                return typeof(TEntity).GetProperties()
                                      .Where(p => p.GetValue(entity) != null &&
                                      !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) &&
                                      !p.Name.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase) &&
                                      !p.Name.Equals("UpdatedAt", StringComparison.OrdinalIgnoreCase))
                                      .ToList();
            }

            public static string FormatFieldsWithEntity(string fields, string entity)
            {
                // Divide la cadena de entrada en elementos separados por coma
                var elements = fields.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                // Formatea cada elemento en el formato deseado "{entidad.Propiedad}"
                var result = string.Join(", ", elements.Select(e => $"{entity}.{e.Trim()}"));

                // Agrega los corchetes de apertura y cierre
                return $"{{{result}}}";
            }

            public static object AnonimousObject(string fields, string entity)
            {
                // Divide la cadena de entrada en elementos separados por coma
                var elements = fields.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                // Formatea cada elemento en el formato deseado "{entidad.Propiedad}"
                var result = elements.ToDictionary(
                    e => e.Trim(),                          // Clave del diccionario: nombre del campo original
                    e => $"{entity}.{e.Trim()}"             // Valor del diccionario: "{entidad.Propiedad}"
                );

                // Devuelve el diccionario como un objeto anónimo
                return new { FormattedFields = result };
            }

            public static string BuildInsertQuery<T>(string tableName, T entity)
            {
                // Obtiene los nombres de las propiedades (columnas) del objeto
                var properties = typeof(T).GetProperties();

                // Construye la lista de columnas y los nombres de los parámetros
                var columns = string.Join(", ", properties.Select(p => $"\"{p.Name}\""));
                var paramNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

                // Construye la consulta SQL
                string query = $@"
                                INSERT INTO ""{tableName}"" ({columns}) 
                                VALUES ({paramNames}) 
                                RETURNING ""Id"";";

                return query;
            }


           

        }
    }

}
