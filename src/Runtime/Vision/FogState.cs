namespace MonoGameMaker.Runtime.Vision
{
    /// <summary>
    /// Estados do autômato celular formal para o sistema de Névoa de Guerra tática.
    /// </summary>
    public enum FogState : byte
    {
        Unexplored = 0,    // Breu total (área inexplorada)
        Explored = 128,    // Penumbra (terreno estático visível, unidades móveis ocultas)
        Visible = 255      // Linha de visão ativa (unidades inimigas e terreno visíveis em tempo real)
    }
}
