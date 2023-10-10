#include "register_types.h"
#include <gdextension_interface.h>
#include <godot_cpp/core/defs.hpp>
#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/godot.hpp>
#include "nativehost.h"
#include "dotnet_api.h"

using namespace godot;

LocalVector<extension_data> extension_datas;
bool initialized = false;

void initialize_fsharp_extension_module(ModuleInitializationLevel p_level) {
    if (!initialized) {
        extension_datas = bind();
        initialized = true;
    }
    for (auto extension_data: extension_datas) {
        extension_data.initialize(p_level);
    }
}

void uninitialize_fsharp_extension_module(ModuleInitializationLevel p_level) {
    for (auto extension_data: extension_datas) {
        extension_data.uninitialize(p_level);
    }
}

extern "C" {
    GDExtensionBool GDE_EXPORT fsharp_extension_init(GDExtensionInterfaceGetProcAddress p_get_proc_address, GDExtensionClassLibraryPtr p_library, GDExtensionInitialization *r_initialization) {
        godot::GDExtensionBinding::InitObject main_init_obj(p_get_proc_address, p_library, r_initialization);
        main_init_obj.register_initializer(initialize_fsharp_extension_module);
        main_init_obj.register_terminator(uninitialize_fsharp_extension_module);
        main_init_obj.set_minimum_library_initialization_level(MODULE_INITIALIZATION_LEVEL_CORE);
        return main_init_obj.init();
//        GDExtensionBool init = main_init_obj.init();
//        WARN_PRINT(godot::Variant(init));
//        return init;

//        auto ret = bind(p_get_proc_address, p_library, r_initialization);
//
//
//        if (ret != 1) {
//            godot::GDExtensionBinding::InitObject main_init_obj(p_get_proc_address, p_library, r_initialization);
//            main_init_obj.register_initializer(initialize_fsharp_extension_module);
//            main_init_obj.register_terminator(uninitialize_fsharp_extension_module);
//            main_init_obj.set_minimum_library_initialization_level(MODULE_INITIALIZATION_LEVEL_CORE);
//            return main_init_obj.init();
//        }
//        return ret;
    }
}
