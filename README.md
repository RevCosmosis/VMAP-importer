VMAP Importer is a scripted importer for Unity that adds support for .vmap files, which are the format used by Valve's Hammer 2 editor. Simply place a .vmap file into your project's assets folder and it will turn into a prefab that can be added to any scene.
This importer doesn't quite work "out of the box" and some setup is required.

THIS IS A WORK IN PROGRESS THAT I MIGHT NOT FINISH. I FORGET WHAT STATE I LEFT IT IN AND IT MIGHT NOT BE FUNCTIONAL WITHOUT SOME FIXING.

## INSTALLATION

Install this package by importing it.

## IMPORTER SETTINGS

The importer comes with a few settings to tweak.

## MATERIAL MAPPING

words words words

## PROP IMPORT

words words words

## CUSTOM ENTITY IMPORT

Importing new types of entities not already supported is relatively easy. Every entity imported is represented by a VMAPObject in code. You can add support for a new type of entity by writing a new class that inherits from VMAPObject, and then adding a case for it in the base importer code. words words words

## LICENSE

This package makes use of [Datamodel.NET](https://github.com/Artfunkel/Datamodel.NET) for importing .vmap files. The library is included as source code in this package for your reference if necessary when writing custom entity import code. (I also had to fix a bug.) Datamodel.NET is provided under MIT License.

This package makes use of [alglib](https://www.alglib.net/) for a particular bit of vector math used in the mesh import process. This library is provided as a .dll. Alglib is provided under GPL2.

My portion of the code is provided under MIT License.

These licenses apply only to the importer itself, not any games or levels produced with the importer. GPL2 is not a very suitable license for commercial games, so it is important that alglibnet2.dll is not included in the final game. Keep everything in the /Editor folder and you'll be fine.

See LICENSE.md for all licenses.
