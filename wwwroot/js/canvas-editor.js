// Board Canvas Studio interactivity.
// Progressive enhancement throughout: every feature here guards itself
// independently, so if one piece of markup isn't on the page (or this script
// fails to load entirely), the plain form-post Add/Delete/Move flow still
// works on its own.
(function () {
    "use strict";

    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenInput ? tokenInput.value : "";

    // ------------------------------------------------------------------
    // Feature 1: drag-to-reorder existing tiles (unchanged from before)
    // ------------------------------------------------------------------
    (function setUpReorder() {
        const grid = document.getElementById("board-canvas-grid");

        if (!grid) {
            return;
        }

        const boardId = grid.dataset.boardId;
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
            if (!draggedTile) {
                return;
            }

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

    // ------------------------------------------------------------------
    // Feature 2: live preview for pasted image URLs, so a broken or
    // hotlink-protected link is obvious before you add the tile.
    // ------------------------------------------------------------------
    (function setUpImagePreview() {
        const input = document.getElementById("ImageUrlValue");
        const preview = document.getElementById("image-url-preview");

        if (!input || !preview) {
            return;
        }

        let debounceTimer = null;

        function showHint(message) {
            preview.innerHTML = "";
            const hint = document.createElement("span");
            hint.className = "image-url-preview-hint";
            hint.textContent = message;
            preview.appendChild(hint);
        }

        function checkUrl(rawUrl) {
            const url = rawUrl.trim();

            if (!url) {
                showHint("Paste a link above to preview it here before adding.");
                return;
            }

            showHint("Loading preview…");

            const img = new Image();

            img.onload = function () {
                preview.innerHTML = "";
                img.alt = "Preview";
                preview.appendChild(img);
            };

            img.onerror = function () {
                showHint(
                    "Couldn't load this image. Try right-clicking the picture itself " +
                    "(not the webpage) and choosing \"Copy image address.\""
                );
            };

            img.src = "/image-proxy?url=" + encodeURIComponent(url);
        }

        input.addEventListener("input", function () {
            window.clearTimeout(debounceTimer);
            debounceTimer = window.setTimeout(function () {
                checkUrl(input.value);
            }, 500);
        });

        if (input.value) {
            checkUrl(input.value);
        }
    })();

    // ------------------------------------------------------------------
    // Feature 3: emoji picker for the Symbol tile type, for anyone whose
    // keyboard/OS doesn't have an easy way to type emoji directly.
    // ------------------------------------------------------------------
    (function setUpEmojiPicker() {
        const symbolInput = document.getElementById("SymbolValue");
        const buttons = document.querySelectorAll(".emoji-picker-btn");

        if (!symbolInput || buttons.length === 0) {
            return;
        }

        buttons.forEach(function (button) {
            button.addEventListener("click", function () {
                symbolInput.value = button.dataset.emoji || button.textContent.trim();
                symbolInput.focus();
            });
        });
    })();

    // ------------------------------------------------------------------
    // Feature 4: drag a tile-type button straight onto the canvas to place
    // a new tile exactly where you want it, instead of it always landing
    // at the end. Dropping selects the matching tile type in the form and
    // remembers the drop position; you still fill in the content and hit
    // "Add Tile" as normal (dragging can't type your quote text for you).
    // ------------------------------------------------------------------
    (function setUpPaletteDragToPlace() {
        const tabs = document.querySelectorAll(".tile-type-tab[draggable='true']");
        const dropTarget =
            document.getElementById("board-canvas-grid") ||
            document.getElementById("board-canvas-empty-dropzone");
        const insertBeforeField = document.getElementById("InsertBeforeItemId");
        const hintEl = document.getElementById("drag-placement-hint");
        const MIME_TYPE = "application/x-bookboard-tile-type";

        if (tabs.length === 0 || !dropTarget) {
            return;
        }

        function clearHighlights() {
            dropTarget.querySelectorAll(".canvas-tile.drop-target-highlight").forEach(function (tile) {
                tile.classList.remove("drop-target-highlight");
            });
            dropTarget.classList.remove("drop-target-highlight");
        }

        function showHint(message) {
            if (!hintEl) {
                return;
            }

            hintEl.textContent = message;
            hintEl.classList.add("visible");

            window.clearTimeout(showHint._timer);
            showHint._timer = window.setTimeout(function () {
                hintEl.classList.remove("visible");
            }, 3200);
        }

        tabs.forEach(function (tab) {
            tab.addEventListener("dragstart", function (event) {
                const type = tab.dataset.tileType;

                if (!type) {
                    return;
                }

                event.dataTransfer.effectAllowed = "copy";
                event.dataTransfer.setData(MIME_TYPE, type);
                // Fallback so browsers that need a generic type still start the drag.
                event.dataTransfer.setData("text/plain", type);
            });
        });

        dropTarget.addEventListener("dragover", function (event) {
            if (!event.dataTransfer.types.includes(MIME_TYPE)) {
                return;
            }

            event.preventDefault();
            event.dataTransfer.dropEffect = "copy";

            clearHighlights();

            const targetTile = event.target.closest(".canvas-tile");

            if (targetTile) {
                targetTile.classList.add("drop-target-highlight");
            } else {
                dropTarget.classList.add("drop-target-highlight");
            }
        });

        dropTarget.addEventListener("dragleave", function (event) {
            if (event.target === dropTarget) {
                clearHighlights();
            }
        });

        dropTarget.addEventListener("drop", function (event) {
            if (!event.dataTransfer.types.includes(MIME_TYPE)) {
                return;
            }

            event.preventDefault();
            clearHighlights();

            const type = event.dataTransfer.getData(MIME_TYPE);
            const radio = document.getElementById("type-" + type);

            if (radio) {
                radio.checked = true;
            }

            const targetTile = event.target.closest(".canvas-tile");

            if (insertBeforeField) {
                insertBeforeField.value = targetTile ? targetTile.dataset.itemId || "" : "";
            }

            const panel = document.getElementById("panel-" + type);
            const focusTarget = panel ? panel.querySelector("input, textarea, select") : null;

            if (focusTarget) {
                focusTarget.focus();
            }

            showHint(
                targetTile
                    ? "Ready — fill in your tile below, it'll be placed right there."
                    : "Ready — fill in your tile below, it'll be added to the canvas."
            );
        });
    })();
})();