using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using MessagePack;
using UnityEngine;
using UnityEngine.UI;

namespace MaximovInk
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager instance;
        public InputField fileName;
        public Transform loadButtonsParent, saveButtonsParent;
        public Button buttonPrefab;

        public InputField seedName;
        public SaveData saveData = new SaveData { seed = 777 };
        
        [MessagePackObject]
        public class SaveData
        {
            [Key(0)] public int seed { get; set; } = 777;
            [Key(1)] public float posX { get; set; }
            [Key(2)] public float posY { get; set; }
        }
        
        public string CurrentSave = "NewGame";
        public string GetSavePath() => applPath + "/saves/" + CurrentSave;

        public string GetTempPath() => applPath + "/saves/temp";

        private string applPath;

        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                instance = this;
                applPath = Application.dataPath;
                if (!Directory.Exists(GetTempPath()))
                {
                    Directory.CreateDirectory(GetTempPath());
                }

                DontDestroyOnLoad(instance);
            }
        }

        public void CheckTempFolder()
        {
            if (!Directory.Exists(GetTempPath()+"/npcs"))
            {
                Directory.CreateDirectory(GetTempPath()+"/npcs");
            }
            if (!Directory.Exists(GetTempPath()+"/chunks"))
            {
                Directory.CreateDirectory(GetTempPath()+"/chunks");
            }
        }

        public void CheckSaveFolder()
        {
            if (!Directory.Exists(GetSavePath()+"/chunks"))
            {
                Directory.CreateDirectory(GetSavePath()+"/chunks");
            }
            if (!Directory.Exists(GetSavePath()+"/npcs"))
            {
                Directory.CreateDirectory(GetSavePath()+"/npcs");
            }
        }
        
        
        
        public void Save()
        {            
            CurrentSave = fileName.text;
            CheckSaveFolder();

            var path = GetTempPath() + "/data";
            ChunkManager.Instance.SaveLoadedChunks();
            var position = GameManager.Instance.player.transform.position;
            saveData.posX = position.x;
            saveData.posY = position.y;
            using (var fs = File.Open(path, FileMode.OpenOrCreate))
            {
                MessagePackSerializer.Serialize(fs, saveData);
            }
            Extenshions.MoveFromTo(GetTempPath(),GetSavePath());
        }

        public void LoadSaveFiles()
        {
            fileName.text = CurrentSave;

            var files = Directory.EnumerateDirectories(Application.dataPath + "/saves")
                .Where(n => Extenshions.NormalizePath(n) != Extenshions.NormalizePath(GetTempPath())).ToList();

            for (var i = 0; i < loadButtonsParent.childCount; i++)
            {
                Destroy(loadButtonsParent.GetChild(i).gameObject);
            }

            for (var i = 0; i < saveButtonsParent.childCount; i++)
            {
                Destroy(saveButtonsParent.GetChild(i).gameObject);
            }

            for (var i = 0; i < files.Count; i++)
            {
                var bload = Instantiate(buttonPrefab, loadButtonsParent);
                var bsave = Instantiate(buttonPrefab, saveButtonsParent);
                var bi = i;

                var name = Path.GetFileName(files[i]);

                bload.GetComponentInChildren<Text>().text = name;
                bload.onClick.AddListener(() => { LoadFile(files[bi]); });
                bsave.GetComponentInChildren<Text>().text = name;
                bsave.onClick.AddListener(() => { fileName.text = name; });
            }
        }

        public void LoadFile(string path)
        {
            CheckTempFolder();
            Extenshions.CleanDirectory(GetTempPath());
            CurrentSave = Path.GetFileName(path);
            if(Directory.Exists(GetSavePath()))
                Extenshions.MoveFromTo(GetSavePath(), GetTempPath());
            
            var data = Path.Combine(path, "data");
            if (File.Exists(data))
            {
                using (var fs = File.Open(data, FileMode.Open))
                {
                    saveData = MessagePackSerializer.Deserialize<SaveData>(fs);
                }
            }

            GameManager.Instance.LoadScene(1);
        }

        public void NewGame()
        {
            saveData.seed = seedName.text == string.Empty ? Extenshions.random.Next(0, 1000000) : int.Parse(seedName.text);
            CheckTempFolder();
            Extenshions.CleanDirectory(GetTempPath());

            GameManager.Instance.LoadScene(1);
        }
    }
}