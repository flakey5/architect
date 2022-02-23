# architect
A simple & lightweight project templating system

## Usage
 * `architect grab [Git repository]` - Download a template to use
 * `architect update [Template name]` - Update a template to latest commit
 * `architect new [Template name]` - Initialize a new project

## Creating a template
 * Familiarize yourself with [Handlebars](https://handlebarsjs.com/guide/).
 * Create an `architect-manifest.yml` file in the main directory of your template and fill it out. This is not included in the generated project.
    * As an example, check out [sample-architect-manifest.yml](./sample-archtiect-manifest.yml)
 * Add the `.template` file extension to all files you wish to be included in the generated project.
    * In these files you are capable of accessing all variables you defined in your Architect Manifest.
 * Commit to a Git repository and you're done!

## TODO
 * Make Git commands cross platform / replace created processes with a library