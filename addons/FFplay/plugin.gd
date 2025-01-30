@tool
extends EditorPlugin

const AUTOLOAD_NAME: String = "FFCore"

var export_handler: FFmpegExportHandler = null

func _enter_tree() -> void:
    export_handler = FFmpegExportHandler.new()
    add_export_plugin(export_handler)
    add_autoload_singleton(AUTOLOAD_NAME, "res://addons/FFplay/FFCore.cs")

func _exit_tree() -> void:
    remove_autoload_singleton(AUTOLOAD_NAME)
    remove_export_plugin(export_handler)
    export_handler = null

# the library don't come out with exported build so we will do this manually thank god godot support this feature
class FFmpegExportHandler extends EditorExportPlugin:
    const LIBARY_PATH: String = "res://addons/FFplay/libs"

    func _export_begin(features: PackedStringArray, is_debug: bool, path: String, flags: int) -> void:
        for file in DirAccess.get_files_at(LIBARY_PATH):
            DirAccess.copy_absolute(LIBARY_PATH + "/" + file, path.get_base_dir() + "/" + file)