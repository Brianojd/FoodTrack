using FoodTrace.API.Domain.Entities;

namespace FoodTrace.API.Application.Services;

public static class ReglasReceta
{
    public static void Publicar(this VersionReceta receta)
    {
        Cantidades.Positiva(receta.Rendimiento);
        if (receta.Ingredientes.Count == 0) throw new ExcepcionRegla("A recipe needs at least one ingredient.");
        if (string.IsNullOrWhiteSpace(receta.Instrucciones)) throw new ExcepcionRegla("Instructions are required.");
        foreach (var ingrediente in receta.Ingredientes) Cantidades.Positiva(ingrediente.Cantidad);
        receta.Publicada = true;
    }

    public static void AsegurarEditable(VersionReceta receta)
    {
        if (receta.Publicada) throw new ExcepcionRegla("Published recipes are immutable. Create a new version.");
    }
}
