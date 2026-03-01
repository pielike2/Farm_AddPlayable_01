namespace Playable.Models
{
    public static class ModelsContainer
    {
        public static InputModel Input { get; private set; }
        public static SceneBasicVariables SceneVariables { get; set; }

        public static void Init()
        {
            Input = new InputModel();
        }
    }
}