# mlstack

This utility effectivly creates a time-machine style backup of a single directory and outputs a single archive file. Layers can be "stacked" on top of each other, creating a staged backup solution.

This utility is similar to other utilities, like borg, or rsnapshot, but it is the simplest possible implemtnation of a staged back-up, and - I say again - compacts all versions of all files into a single globular file. This means that your single stacked history of your project files could be transferred, backed-up itself, synced using syncthing, etc. etc.

The command line program is in version 1.0, meaning that there is a lot more to add in future versions. Since this program is written entierly in c# a GUI could be made, for example, but my plan is to create an open-source backup server using my networking code (not in this project) since I run a bunch of little raspberrypis for various purposes.

# Installation

If you're not interested in the source-code stuff then just download the latest release version and pop it in /usr/bin or similar, then run using the commmand 'mlstack'.

# Usage

## Quick Start

```
mlstack stack --new /path/to/stack.stack /folder/to/stack
```

## Basic Usage

```
mlstack <command> [switches] <stack> [arguments]
```

The following commands are currently in use:

- init
- stack
- list-levels
- prune
- delete-level
- recover-file

Pass --help for a full list of commands and switches.
