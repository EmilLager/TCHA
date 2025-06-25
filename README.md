A grid based draggable image system.

ImageGrid.cs manages the grid and input logic.
- manages instances of the cell prefab
- handles input
- handles Image fetching
- Contains utilities for transforming screen to image and to grid space and other utilities

Some of these could be refactored out as this is the largest class in the project and in a bigger case I would do that
ImageGridCell.cs controls a single grid cell instance and could be made into a pure class
with an injected monobehavior for better performance due to less needed prefab instances
- handles references to the cells component
- handles position manipulation

ImageCollectionAsset.cs a scriptable object used as an image database.
- handles source image storing and management
- handles image fetching on request
Could be injected into the ImageGrid instead of a direct reference
Easily switchable, configurable and extendable
