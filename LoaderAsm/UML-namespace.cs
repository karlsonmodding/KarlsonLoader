using System;

namespace UML
{
    public interface IMod
    {
        void Start();
        void Update(float deltaTime);
        void FixedUpdate(float fixedDeltaTime);
        void OnGUI();
        void OnApplicationQuit();
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ModInfo : Attribute
    {
        public ModInfo(string _name, string _author, string _version)
        {
            name = _name;
            guid = string.Join(".", _name.Split(' '));
            author = _author;
            version = _version;
            dependencies = Array.Empty<string>();
            modType = ModType.ClientOnly;
        }

        public ModInfo(string _guid, string _name, string _author, string _version)
        {
            guid = _guid;
            name = _name;
            author = _author;
            version = _version;
            dependencies = Array.Empty<string>();
            modType = ModType.ClientOnly;
        }

        public ModInfo(string _guid, string _name, string _author, string _version, string[] _dependencies)
        {
            guid = _guid;
            name = _name;
            author = _author;
            version = _version;
            dependencies = _dependencies;
            modType = ModType.ClientOnly;
        }

        public ModInfo(string _guid, string _name, string _author, string _version, string[] _dependencies, ModType _modType)
        {
            guid = _guid;
            name = _name;
            author = _author;
            version = _version;
            dependencies = _dependencies;
            modType = _modType;
        }

        public readonly string name;
        public readonly string author;
        public readonly string version;
        public readonly string guid;
        public readonly string[] dependencies;
        public readonly ModType modType;
    }

    // natievly UML / KarlsonLoader doesn't know the concept of multiplayer, it remains to the mod adding the multiplayer capability to implement it
    public enum ModType
    {
        ClientOnly,
        Multiplayer,
    }
}