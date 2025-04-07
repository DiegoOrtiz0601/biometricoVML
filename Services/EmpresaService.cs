using System.Collections.Generic;
using System.Linq;
using BiomentricoHolding.Data; // Ajusta esto según dónde esté tu DbContext
using BiomentricoHolding.Data.DataBaseRegistro_Test; // Namespace donde quedó la clase `Empresa`

namespace BiomentricoHolding.Services
{
    public static class EmpresaService
    {
        public static List<Empresa> ObtenerEmpresas()
        {
            using (var context = AppSettings.GetContextUno())
            {
                return context.Empresas
                    .OrderBy(e => e.Nombre) // opcional
                    .ToList();
            }
        }
    }
}
