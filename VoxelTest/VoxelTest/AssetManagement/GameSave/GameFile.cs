﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO.Compression;

namespace DwarfCorp
{

    public class GameFile : SaveData
    {
        public class GameData
        {
            public Texture2D Screenshot { get; set; }
            public List<ChunkFile> ChunkData { get; set; }
            public MetaData Metadata { get; set; }

            public OrbitCamera Camera { get; set; }

            public ComponentManager Components { get; set; }

            public GameData()
            {
                Metadata = new MetaData();
            }

            public void SaveToDirectory(string directory)
            {
                System.IO.Directory.CreateDirectory(directory);
                System.IO.Directory.CreateDirectory(directory + Program.DirChar + "Chunks");

                foreach(ChunkFile chunk in ChunkData)
                {
                    chunk.WriteFile(directory + Program.DirChar + "Chunks" + Program.DirChar + chunk.ID.X + "_" + chunk.ID.Y + "_" + chunk.ID.Z + "." + ChunkFile.CompressedExtension, true);
                }

                Metadata.WriteFile(directory + Program.DirChar + "MetaData." + MetaData.CompressedExtension, true);
                
                FileUtils.SaveJSon(Camera, directory + Program.DirChar + "Camera." + "json", false);

                FileUtils.SaveJSon(Components, directory + Program.DirChar + "Components." + "zcomp", true);
            }
        }

        public GameData Data { get; set; }

        public new static string Extension = "game";
        public new static string CompressedExtension = "zgame";

        public GameFile(string overworld)
        {
            Data = new GameData
            {
                Metadata =
                {
                    OverworldFile = overworld,
                    WorldOrigin = PlayState.WorldOrigin,
                    WorldScale = PlayState.WorldScale,
                    TimeOfDay = PlayState.Sky.TimeOfDay,
                    ChunkHeight = GameSettings.Default.ChunkHeight,
                    ChunkWidth = GameSettings.Default.ChunkWidth,
                },
                Camera = PlayState.Camera,
                Components = PlayState.ComponentManager,
                ChunkData = new List<ChunkFile>()
            };


            foreach(ChunkFile file in PlayState.ChunkManager.ChunkData.ChunkMap.Select(pair => new ChunkFile(pair.Value)))
            {
                Data.ChunkData.Add(file);
            }

        }

        public virtual string GetExtension()
        {
            return "game";
        }

        public virtual string GetCompressedExtension()
        {
            return "zgame";
        }

        public GameFile(string file, bool compressed)
        {
            Data = new GameData();
            ReadFile(file, compressed);
        }

        public GameFile()
        {
            Data = new GameData();
        }

        public void CopyFrom(GameFile file)
        {
            Data = file.Data;
        }

        public bool LoadComponents(string filePath)
        {
            string[] componentFiles = GetFilesInDirectory(filePath, true, "zcomp", "zcomp");
            if (componentFiles.Length > 0)
            {
                Data.Components = FileUtils.LoadJson<ComponentManager>(componentFiles[0], true);
            }
            else
            {
                return false;
            }

            return true;
        }

        public override sealed bool ReadFile(string filePath, bool isCompressed)
        {
            if(!System.IO.Directory.Exists(filePath))
            {
                return false;
            }
            else
            {
                string[] screenshots = GetFilesInDirectory(filePath, false, "png", "png");
                string[] metaFiles = GetFilesInDirectory(filePath, isCompressed, GameFile.MetaData.CompressedExtension, GameFile.MetaData.Extension);
                string[] cameraFiles = GetFilesInDirectory(filePath, false, "json", "json");
                if(metaFiles.Length > 0)
                {
                    Data.Metadata = new MetaData(metaFiles[0], isCompressed);
                }
                else
                {
                    return false;
                }

                if(cameraFiles.Length > 0)
                {
                    Data.Camera = FileUtils.LoadJson<OrbitCamera>(cameraFiles[0], false);
                }
                else
                {
                    return false;
                }


                string[] chunkDirs = System.IO.Directory.GetDirectories(filePath, "Chunks");

                if(chunkDirs.Length > 0)
                {
                    string chunkDir = chunkDirs[0];

                    string[] chunks = ChunkFile.GetFilesInDirectory(chunkDir, isCompressed, ChunkFile.CompressedExtension, ChunkFile.Extension);
                    Data.ChunkData = new List<ChunkFile>();
                    foreach(string chunk in chunks)
                    {
                        Data.ChunkData.Add(new ChunkFile(chunk, isCompressed));
                    }
                }
                else
                {
                    return false;
                }

                if(screenshots.Length > 0)
                {
                    string screenshot = screenshots[0];
                    Data.Screenshot = TextureManager.LoadInstanceTexture(screenshot);
                }

                return true;
            }
        }

        public override bool WriteFile(string filePath, bool compress)
        {
            Data.SaveToDirectory(filePath);
            return true;
        }

        public class MetaData : SaveData
        {
            public string OverworldFile { get; set; }
            public float WorldScale { get; set; }
            public Vector2 WorldOrigin { get; set; }
            public int ChunkWidth { get; set; }
            public int ChunkHeight { get; set; }
            public float TimeOfDay { get; set; }

            public new static string Extension = "meta";
            public new static string CompressedExtension = "zmeta";

            public MetaData(string file, bool compressed)
            {
                ReadFile(file, compressed);
            }


            public MetaData()
            {
            }

            public void CopyFrom(MetaData file)
            {
                WorldScale = file.WorldScale;
                WorldOrigin = file.WorldOrigin;
                ChunkWidth = file.ChunkWidth;
                ChunkHeight = file.ChunkHeight;
                TimeOfDay = file.TimeOfDay;
                OverworldFile = file.OverworldFile;
            }

            public override sealed bool ReadFile(string filePath, bool isCompressed)
            {
                MetaData file = FileUtils.LoadJson<MetaData>(filePath, isCompressed);

                if(file == null)
                {
                    return false;
                }
                else
                {
                    CopyFrom(file);
                    return true;
                }
            }

            public override bool WriteFile(string filePath, bool compress)
            {
                return FileUtils.SaveJSon(this, filePath, compress);
            }
        }
    }

}