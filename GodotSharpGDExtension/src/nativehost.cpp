// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Standard headers
#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <assert.h>
#include <iostream>

#include <nethost.h>

#include <coreclr_delegates.h>
#include <hostfxr.h>
#include "dotnet_api.h"
#include "gdextension_interface.h"
#include "godot_cpp/core/error_macros.hpp"
#include "godot_cpp/godot.hpp"
#include "godot_cpp/variant/variant.hpp"
#include "godot_cpp/variant/string.hpp"
#include "godot_cpp/templates/local_vector.hpp"
#include <filesystem>

#include "register_types.h"
#define TOML_EXCEPTIONS 0
#include "tomlplusplus/toml.hpp"
#include "godot_cpp/variant/string.hpp"
#include "godot_cpp/variant/variant.hpp"
using namespace std::string_view_literals;


#ifdef _WIN32
#include <Windows.h>
#include <sstream>

#define STR(s) L ## s
#define CH(c) L ## c
#define DIR_SEPARATOR L'\\'

#else
#include <dlfcn.h>
#include <limits.h>

#define STR(s) s
#define CH(c) c
#define DIR_SEPARATOR '/'
#define MAX_PATH PATH_MAX

#endif

using string_t = std::basic_string<char_t>;

namespace
{
    // Globals to hold hostfxr exports
    hostfxr_initialize_for_runtime_config_fn init_fptr;
    hostfxr_get_runtime_delegate_fn get_delegate_fptr;
    hostfxr_close_fn close_fptr;

    // Forward declarations
    bool load_hostfxr();
    void* get_current_module_handle();

    load_assembly_and_get_function_pointer_fn get_dotnet_load_assembly(const char_t *assembly);
    void *load_library(const char_t *);

}

void bind()
{
#ifdef _WIN32
    load_library(STR("fsharp.dll"));
#else
    load_library(STR("libfsharp.so"));
#endif
//    godot::LocalVector<extension_data> extension_datas;
    WARN_PRINT("Test");
    string_t root_path = STR("./");
    WARN_PRINT(godot::String(std::filesystem::current_path().c_str()));

    //
    // STEP 1: Load HostFxr and get exported hosting functions
    //
    if (!load_hostfxr())
    {
        assert(false && "Failure: load_hostfxr()");
    }

    //
    // STEP 2: Initialize and start the .NET Core runtime
    //
    const string_t  config_path = root_path + STR("GodotSharpGDExtension.CSharp.runtimeconfig.json");
    load_assembly_and_get_function_pointer_fn load_assembly_and_get_function_pointer = nullptr;
    load_assembly_and_get_function_pointer = get_dotnet_load_assembly(config_path.c_str());
    assert(load_assembly_and_get_function_pointer != nullptr && "Failure: get_dotnet_load_assembly()");

    //
    // STEP 3: Load managed assembly and get function pointer to a managed method
    //
    const string_t godot_sharp_extension_path = root_path + STR("ScriptInterop.CSharp.dll");

    WARN_PRINT(godot::Variant(std::filesystem::exists(std::filesystem::path(godot_sharp_extension_path))));

    const char_t *main_dotnet_type = STR("ScriptInterop.CSharp.Entry, ScriptInterop.CSharp");
    const char_t *bind_method = STR("Initialize");
    // <SnippetLoadAndGet>
    // Function pointer to managed delegate


    typedef void (CORECLR_DELEGATE_CALLTYPE *main_bind_function)();

    main_bind_function bind = nullptr;
    int rc = load_assembly_and_get_function_pointer(
            godot_sharp_extension_path.c_str(),
            main_dotnet_type,
            bind_method,
            UNMANAGEDCALLERSONLY_METHOD /*delegate_type_name*/,
            nullptr,
            (void**)&bind);

    // </SnippetLoadAndGet>
    if (rc != 0 || bind == nullptr) {
        std::stringstream message;
        message << "load_assembly_and_get_function_pointer failed: " << std::hex << std::showbase << rc << std::endl;
        WARN_PRINT(godot::String(message.str().c_str()));
        WARN_PRINT(godot::Variant(rc));
//        return extension_datas;
        return;
    }

//    dlopen(NULL, RTLD_LAZY | RTLD_LOCAL);

    auto funcs = interface_functions();
    init_interface_functions(&funcs);

    WARN_PRINT("Calling bind");

    bind();
//    WARN_PRINT(godot::Variant(string));

    WARN_PRINT("Success");

//    extension_datas.push_back(*main_extension_data);

    WARN_PRINT("Added extension data");
    return ;

//    WARN_PRINT("Loaded main sharp extension");

    toml::parse_result sharp_extensions_data = toml::parse_file("sharp_extensions.toml");

    if (sharp_extensions_data.failed()) {
//        WARN_PRINT("Could not load sharp_extensions.toml");
        return ;
    }

    auto table = sharp_extensions_data.table();

    auto extensions = *table["extensions"].as_table();

    typedef struct extension_data* (CORECLR_DELEGATE_CALLTYPE *bind_function)(interface_functions*);

    for (auto extension : extensions) {
        auto extension_data_entires = *extension.second.as_table();
        auto library = *extension_data_entires["library"].value<string_t>();
        auto entry_class = *extension_data_entires["entry_class"].as_string();

        const string_t extension_library_path = root_path + library;
//        const char_t *extension_dotnet_type = entry_class->c_str();
        bind_function extension_bind = nullptr;
        rc = load_assembly_and_get_function_pointer(
                extension_library_path.c_str(),
                reinterpret_cast<const char_t*>(entry_class->c_str()),
                bind_method,
                UNMANAGEDCALLERSONLY_METHOD /*delegate_type_name*/,
                nullptr,
                (void**)&extension_bind);

        // </SnippetLoadAndGet>
        if (rc != 0 || extension_bind == nullptr) {
            std::cerr << "load_assembly_and_get_function_pointer failed: " << std::hex << std::showbase << rc << std::endl;
            return ;
        }

        auto extension_data = extension_bind(&funcs);

//        extension_datas.push_back(*extension_data);

    }
//    WARN_PRINT("Loaded sharp extensions");
    return;
}

/********************************************************************************************
 * Function used to load and activate .NET Core
 ********************************************************************************************/

namespace
{
    // Forward declarations
    void *get_export(void *, const char *);

#ifdef _WIN32
    void *load_library(const char_t *path)
    {
//        WARN_PRINT(path);
        HMODULE h = ::LoadLibraryW(path);
        assert(h != nullptr);
        return (void*)h;
    }
    void *get_export(void *h, const char *name)
    {
        void *f = ::GetProcAddress((HMODULE)h, name);
        assert(f != nullptr);
        return f;
    }
    void* get_current_module_handle() {
        HMODULE hModule = NULL;
        GetModuleHandleEx(
                GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS,
                (LPCTSTR)get_current_module_handle,
                &hModule);
        assert(hModule != nullptr);
        return (void*)hModule;
    }
#else
    void *load_library(const char_t *path)
    {
        void *h = dlopen(path, RTLD_LAZY | RTLD_LOCAL);
        assert(h != nullptr);

        return h;
    }
    void *get_export(void *h, const char *name)
    {
        void *f = dlsym(h, name);
        assert(f != nullptr);
        return f;
    }
    void* get_current_module_handle() {
        void *h = dlopen(nullptr, RTLD_LAZY);
        assert(h != nullptr);

        Dl_info info;
        if (dladdr(h, &info) == 0) {
//            WARN_PRINT("Failed to retrieve information about shared library");
            return h;
        }

//        WARN_PRINT(info.dli_fname);
        return h;
    }
#endif

    // <SnippetLoadHostFxr>
    // Using the nethost library, discover the location of hostfxr and get exports
    bool load_hostfxr()
    {
        // Load hostfxr and get desired exports
//        WARN_PRINT(godot::String(HOSTFXR_PATH));
        void *lib = load_library(HOSTFXR_PATH);
        init_fptr = (hostfxr_initialize_for_runtime_config_fn)get_export(lib, "hostfxr_initialize_for_runtime_config");
        get_delegate_fptr = (hostfxr_get_runtime_delegate_fn)get_export(lib, "hostfxr_get_runtime_delegate");
        close_fptr = (hostfxr_close_fn)get_export(lib, "hostfxr_close");

        return (init_fptr && get_delegate_fptr && close_fptr);
    }
    // </SnippetLoadHostFxr>

    // <SnippetInitialize>
    // Load and initialize .NET Core and get desired function pointer for scenario
    load_assembly_and_get_function_pointer_fn get_dotnet_load_assembly(const char_t *config_path)
    {
        // Load .NET Core
        void *load_assembly_and_get_function_pointer = nullptr;
        hostfxr_handle cxt = nullptr;
        int rc = init_fptr(config_path, nullptr, &cxt);
        if (rc != 0 || cxt == nullptr)
        {
            std::cerr << "Init failed: " << std::hex << std::showbase << rc << std::endl;
            close_fptr(cxt);
            return nullptr;
        }

        // Get the load assembly function pointer
        rc = get_delegate_fptr(
                cxt,
                hdt_load_assembly_and_get_function_pointer,
                &load_assembly_and_get_function_pointer);
        if (rc != 0 || load_assembly_and_get_function_pointer == nullptr)
            std::cerr << "Get delegate failed: " << std::hex << std::showbase << rc << std::endl;

        close_fptr(cxt);
        return (load_assembly_and_get_function_pointer_fn)load_assembly_and_get_function_pointer;
    }
    // </SnippetInitialize>


}
