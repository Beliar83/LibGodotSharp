using GDExtension;
using System.Reflection;
using Godot;
using Godot.Bridge;
using Godot.Collections;
using Godot.NativeInterop;
using static GDExtension.Native;

namespace LibGodotSharp
{
    public static unsafe class LibGodotManager
    {
        public delegate void SceneTreeLoad(SceneTree scene);

        public delegate Dictionary ProjectSettingsLoad();
        
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void ProjectSettingsLoadNative(void* scene);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void SceneTreeLoadNative(void* scene);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool GDentryPoint(GDExtensionInterfaceGetProcAddress getProcAddress, void* library, GDExtensionInitialization* expIntilzation);

        // [DllImport("godot_android", EntryPoint = "libgodot_bind", CallingConvention = CallingConvention.StdCall)]
        // internal static extern void Android_libgodot_bind(void* entryPoint, void* sceneTreeLoad, void* projectSettingsLoad);
        //
        [DllImport("libgodot", EntryPoint = "libgodot_bind", CallingConvention = CallingConvention.StdCall)]
        internal static extern void libgodot_bind_for_editor(void* sceneTreeLoad, void* projectSettingsLoad, GodotPluginsInitializeForGameBuild godotPluginsInitialize);
        
        [DllImport("libgodot", EntryPoint = "libgodot_bind", CallingConvention = CallingConvention.StdCall)]
        internal static extern void libgodot_bind_for_game(void* sceneTreeLoad, void* projectSettingsLoad, GodotPluginsInitializeForGameBuild godotPluginsInitialize);

        [DllImport("libgodot", CallingConvention = CallingConvention.StdCall)]
        internal static extern int setup_main(int amount, string[] args);
        [DllImport("libgodot", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int godot_main();
        [DllImport("libgodot", CallingConvention = CallingConvention.StdCall)]
        internal static extern bool is_editor_build();


        
        [StructLayout(LayoutKind.Sequential)]
        internal struct PluginsCallbacks
        {
            // public delegate bool LoadScriptAssembliesCallback();
            // public delegate IntPtr GetEditorPluginCallback(int arg0, IntPtr arg1);
            // public delegate bool UnloadScriptAssembliesCallback();
            
            // public LoadScriptAssembliesCallback* LoadScriptAssembliesCallback;
            // public GetEditorPluginCallback GetEditorPluginCallback;
            // public UnloadScriptAssembliesCallback UnloadScriptAssembliesCallback;
            public delegate* unmanaged<godot_bool> LoadScriptAssembliesCallback;
            public delegate* unmanaged<IntPtr, int, IntPtr> GetEditorPluginCallback;
            public delegate* unmanaged<godot_bool> UnloadScriptAssembliesCallback;            
        }        
        
        internal delegate bool GodotPluginsInitializeForEditorBuild(IntPtr godotDllHandle, bool isEditorHint, PluginsCallbacks *pluginsCallbacks, 
            ManagedCallbacks* managedCallbacks, IntPtr unmanagedCallbacks, int unmanagedCallbacksSize);

        internal delegate bool GodotPluginsInitializeForGameBuild(IntPtr godotDllHandle, ManagedCallbacks* managedCallbacks, IntPtr unmanagedCallbacks, int unmanagedCallbacksSize);

        internal static SceneTreeLoad _sceneTreeLoad;
        internal static ProjectSettingsLoad _projectSettingsLoad;

        internal static void SceneTreeMain(godot_variant* startup)
        {
            // var ptr = new IntPtr(startup);
            // NativeFuncs.godotsharp_variant_new_object(out godot_variant tree, ptr);
            var tree = VariantUtils.ConvertTo<SceneTree>(*startup);
            _sceneTreeLoad(tree);
            GC.Collect();
        }

        internal unsafe static void ProjectSettingsMain(godot_dictionary* settings)
        {
            Dictionary projectSettings = _projectSettingsLoad();
            
            NativeFuncs.godotsharp_dictionary_merge(ref *settings, VariantUtils.CreateFromDictionary(projectSettings).Dictionary, godot_bool.True);
        }        
        
        private static Assembly[] _scriptAssemblies;
        
        [UnmanagedCallersOnly]
        static godot_bool LoadScriptAssemblies()
        {
            return LoadScripts() ? godot_bool.True : godot_bool.False;
        }

        static bool LoadScripts()
        {
            foreach (Assembly scriptAssembly in _scriptAssemblies)
            {
                ScriptManagerBridge.LookupScriptsInAssembly(scriptAssembly);
            }

            return true;
        }
        
        public static int RunGodot(string[] args, SceneTreeLoad sceneTreeLoad, ProjectSettingsLoad projectSettingsLoad, Assembly[] scriptAssemblies, bool verboes = false)
        {
            if (sceneTreeLoad is null)
            {
                throw new Exception("Needs sceneTreeLoad");
            }

            if (_sceneTreeLoad is not null)
            {
                throw new Exception("All ready bound into godot");
            }

            _scriptAssemblies = scriptAssemblies;
            _sceneTreeLoad = sceneTreeLoad;
            _projectSettingsLoad = projectSettingsLoad;

            var sceneTreeMainPointer = (void*)SaftyRapper.GetFunctionPointerForDelegate(SceneTreeMain);
            var projectSettingsMainPointer = (void*)SaftyRapper.GetFunctionPointerForDelegate(ProjectSettingsMain);

            CheckIfLatestLibraryInRoot();
            try
            {
                if (is_editor_build())
                {
                    libgodot_bind_for_game(sceneTreeMainPointer, projectSettingsMainPointer, InitializeMonoForTemplateBuild);
                }
                else
                {
                    libgodot_bind_for_editor(sceneTreeMainPointer, projectSettingsMainPointer, InitializeMonoForTemplateBuild);
                }
            }
            catch (DllNotFoundException)
            {
                FallBackLoadPlatformLibrary();
                if (is_editor_build())
                {
                    libgodot_bind_for_game(sceneTreeMainPointer, projectSettingsMainPointer, InitializeMonoForTemplateBuild);
                }
                else
                {
                    libgodot_bind_for_editor(sceneTreeMainPointer, projectSettingsMainPointer, InitializeMonoForTemplateBuild);
                }
            }

            var argss = new List<string>(args);
            argss.Insert(0, "libgodot");
            if (verboes)
            {
                argss.Add("--verbose");
            }


            string[] strings = argss.ToArray();
            int result = setup_main(argss.Count, strings);
            
            
            if (result != 0) return result;
            return godot_main();
        }

        private static bool InitializeMonoForEditorBuild(IntPtr godotDllHandle, bool isEditorHint, PluginsCallbacks* pluginsCallbacks, ManagedCallbacks* managedCallbacks, IntPtr unmanagedCallbacks, int unmanagedCallbacksSize)
        {
            NativeFuncs.Initialize(unmanagedCallbacks, unmanagedCallbacksSize);
            *pluginsCallbacks = new PluginsCallbacks
            {
                LoadScriptAssembliesCallback = &LoadScriptAssemblies,
            };

            *managedCallbacks = ManagedCallbacks.Create();
            return true;
        }                
        private static bool InitializeMonoForTemplateBuild(IntPtr godotDllHandle, ManagedCallbacks* managedCallbacks, IntPtr unmanagedCallbacks, int unmanagedCallbacksSize)
        {
            NativeFuncs.Initialize(unmanagedCallbacks, unmanagedCallbacksSize);
            *managedCallbacks = ManagedCallbacks.Create();
            LoadScripts();
            return true;
        }        

        private static bool FileCompare(string file1, string file2)
        {
            int file1byte;
            int file2byte;
            FileStream fs1;
            FileStream fs2;

            // Determine if the same file was referenced two times.
            if (file1 == file2)
            {
                // Return true to indicate that the files are the same.
                return true;
            }

            // Open the two files.
            fs1 = new FileStream(file1, FileMode.Open);
            fs2 = new FileStream(file2, FileMode.Open);

            // Check the file sizes. If they are not the same, the files
            // are not the same.
            if (fs1.Length != fs2.Length)
            {
                // Close the file
                fs1.Close();
                fs2.Close();

                // Return false to indicate files are different
                return false;
            }

            // Read and compare a byte from each file until either a
            // non-matching set of bytes is found or until the end of
            // file1 is reached.
            do
            {
                // Read one byte from each file.
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            // Close the files.
            fs1.Close();
            fs2.Close();

            // Return the success of the comparison. "file1byte" is
            // equal to "file2byte" at this point only if the files are
            // the same.
            return ((file1byte - file2byte) == 0);
        }

        private static void CheckIfLatestLibraryInRoot()
        {
            var targetPath = GetRuntimePath();
            if (targetPath is null)
            {
                return;
            }
            var rootFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "libgodot" + Path.GetExtension(targetPath));
            if (!File.Exists(rootFile))
            {
                return;
            }
            if (FileCompare(targetPath, rootFile))
            {
                return;
            }
            File.Delete(rootFile);
        }

        private static string GetRuntimePath()
        {
            var arch = RuntimeInformation.ProcessArchitecture.ToString().ToLower();
            string neededPlatfomFile;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                neededPlatfomFile = Path.Combine($"win-{arch}", "native", "libgodot.dll");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                neededPlatfomFile = Path.Combine($"linux-{arch}", "native", "libgodot.so");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                neededPlatfomFile = Path.Combine($"osx-{arch}", "native", "libgodot.dylib");
            }
            else
            {
                neededPlatfomFile = Path.Combine($"{RuntimeInformation.OSDescription.ToLower()}-{arch}", "native", "libgodot.so");
            }
            var runtimesFolder = Path.Combine(Directory.GetCurrentDirectory(), "runtimes", neededPlatfomFile);
            if (!File.Exists(runtimesFolder))
            {
                runtimesFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "runtimes", neededPlatfomFile);
            }
            if (!File.Exists(runtimesFolder))
            {
                runtimesFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(typeof(LibGodotManager)).Location), "runtimes", neededPlatfomFile);
            }
            if (!File.Exists(runtimesFolder))
            {
                runtimesFolder = null;
            }
            return runtimesFolder;
        }

        private static void FallBackLoadPlatformLibrary()
        {
            var arch = RuntimeInformation.ProcessArchitecture.ToString().ToLower();
            string neededPlatfomFile = GetRuntimePath();
            if (neededPlatfomFile is null)
            {
                return;
            }
            var runtimesFolder = Path.Combine(Directory.GetCurrentDirectory(), "runtimes", neededPlatfomFile);
            if (!File.Exists(runtimesFolder))
            {
                runtimesFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "runtimes", neededPlatfomFile);
            }
            if (!File.Exists(runtimesFolder))
            {
                runtimesFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(typeof(LibGodotManager)).Location), "runtimes", neededPlatfomFile);
            }
            if (!File.Exists(runtimesFolder))
            {
                throw new DllNotFoundException($"Failed to find libgodot for {RuntimeInformation.OSDescription.ToLower()} arc:{RuntimeInformation.ProcessArchitecture.ToString().ToLower()}");
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    LoadLibraryWindows(runtimesFolder, arch);
                }
                catch
                {
                    Console.WriteLine($"Failed to load library natively Target:{runtimesFolder} now lazy loading");
                    LazyLoadLibrary(runtimesFolder);
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try
                {
                    LoadLibraryLinux(runtimesFolder, arch);
                }
                catch
                {
                    Console.WriteLine($"Failed to load library natively Target:{runtimesFolder} now lazy loading");
                    LazyLoadLibrary(runtimesFolder);
                }
            }
            else
            {
                LazyLoadLibrary(runtimesFolder);
            }
        }

        private static void LazyLoadLibrary(string library)
        {
            var ex = Path.GetExtension(library);
            File.Copy(library, Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "libgodot" + ex), true);
        }

        [DllImport("libdl", CharSet = CharSet.Unicode)]
        static extern IntPtr dlopen(string fileName, int flags);

        private static void LoadLibraryLinux(string library, string arch)
        {
            const int RTLD_NOW = 2;
            if (!(
                   dlopen(library, RTLD_NOW) != IntPtr.Zero
                || dlopen($"./runtimes/linux-{arch}/native/libgodot.so", RTLD_NOW) != IntPtr.Zero
                || dlopen("libgodot.so", RTLD_NOW) != IntPtr.Zero))
            {
                throw new DllNotFoundException(library);
            }
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern IntPtr LoadLibraryW(string fileName);

        private static void LoadLibraryWindows(string library, string arch)
        {
            if (!(
                    LoadLibraryW(library) != IntPtr.Zero
                 || LoadLibraryW($"runtimes/win-{arch}/native/libgodot.dll") != IntPtr.Zero
                 || LoadLibraryW("libgodot") != IntPtr.Zero)
               )
            {
                throw new DllNotFoundException(library);
            }
        }


    }
}
