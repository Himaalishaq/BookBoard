// Drag-and-drop reordering for the CustomizeBoard canvas grid.
// Progressive enhancement: if this script fails to load, tiles still work
// (they just keep whatever order they were added in) and Add/Delete still
// work as normal form posts.
(function () {
    "use strict";

    const grid = document.getElementById("board-canvas-grid");

    if (!grid) {
        return;
    }

    const boardId = grid.dataset.boardId;
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenInput ? tokenInput.value : "";
    const statusEl = document.getElementById("canvas-save-status");

    let draggedTile = null;

    function getTiles() {
        return Array.from(grid.querySelectorAll(".canvas-tile"));
    }

    function setStatus(message, isError) {
        if (!statusEl) {
            return;
        }

        statusEl.textContent = message;
        statusEl.classList.toggle("canvas-save-status-error", Boolean(isError));

        if (!isError && message) {
            window.clearTimeout(setStatus._timer);
            setStatus._timer = window.setTimeout(function () {
                statusEl.textContent = "";
            }, 1800);
        }
    }

    function handleDragStart(event) {
        const tile = event.target.closest(".canvas-tile");

        if (!tile) {
            return;
        }

        draggedTile = tile;
        tile.classList.add("dragging");
        event.dataTransfer.effectAllowed = "move";

        // Firefox requires setData to be called for drag to initiate.
        event.dataTransfer.setData("text/plain", tile.dataset.itemId || "");
    }

    function handleDragOver(event) {
        if (!draggedTile) {
            return;
        }

        event.preventDefault();
        event.dataTransfer.dropEffect = "move";

        const targetTile = event.target.closest(".canvas-tile");

        if (!targetTile || targetTile === draggedTile) {
            return;
        }

        const rect = targetTile.getBoundingClientRect();
        const isBefore = event.clientX < rect.left + rect.width / 2;

        getTiles().forEach(function (tile) {
            tile.classList.remove("drag-over");
        });
        targetTile.classList.add("drag-over");

        if (isBefore) {
            grid.insertBefore(draggedTile, targetTile);
        } else {
            grid.insertBefore(draggedTile, targetTile.nextElementSibling);
        }
    }

    function handleDrop(event) {
        event.preventDefault();
    }

    function handleDragEnd() {
        getTiles().forEach(function (tile) {
            tile.classList.remove("dragging");
            tile.classList.remove("drag-over");
        });

        draggedTile = null;
        saveOrder();
    }

    async function saveOrder() {
        const orderedIds = getTiles()
            .map(function (tile) {
                return parseInt(tile.dataset.itemId, 10);
            })
            .filter(function (id) {
                return !Number.isNaN(id);
            });

        setStatus("Saving order…", false);

        try {
            const response = await fetch(
                window.location.pathname + "?handler=Reorder&id=" + encodeURIComponent(boardId),
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "X-CSRF-TOKEN": token
                    },
                    body: JSON.stringify({ orderedIds: orderedIds })
                }
            );

            if (!response.ok) {
                throw new Error("Request failed with status " + response.status);
            }

            setStatus("Order saved ✓", false);
        } catch (error) {
            setStatus("Couldn't save the new order — try again.", true);
        }
    }

    grid.addEventListener("dragstart", handleDragStart);
    grid.addEventListener("dragover", handleDragOver);
    grid.addEventListener("drop", handleDrop);
    grid.addEventListener("dragend", handleDragEnd);
})();