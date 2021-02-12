# VMAP IMPORTER FOR UNITY

VMAP Importer is a scripted importer for Unity that adds support for .vmap files, which are the format used by Valve's Hammer 2 editor. Simply place a .vmap file into your project's assets folder and it will turn into a prefab that can be added to any scene.
This importer doesn't quite work "out of the box" and some setup is required.

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