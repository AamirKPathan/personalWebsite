using System;
using System.IO;
using System.Text;

var outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "BulkGPT");

Directory.CreateDirectory(outputDirectory);

const string ManifestJson = """
{
  "manifest_version": 3,
  "name": "BulkGPT",
  "version": "2.1",
  "description": "Bulk delete and archive ChatGPT conversations using shift-click selection.",
  "permissions": ["activeTab", "scripting"],
  "content_scripts": [
    {
      "matches": [
        "https://chat.openai.com/*",
        "https://chatgpt.com/*"
      ],
      "js": ["content.js"],
      "css": ["styles.css"]
    }
  ]
}
""";

const string ContentJs = """
(function () {

  let selectedChats = [];
  let lastClickedIndex = null;

  const observer = new MutationObserver(() => {
    if (document.body && !document.getElementById("bulkGPTOverlay")) {
      observer.disconnect();
      initBulkGPT();
    }
  });

  observer.observe(document.documentElement, { childList: true, subtree: true });

  function initBulkGPT() {
    setTimeout(() => {
      createOverlay();
      hookSidebarClicks();
    }, 800);
  }

  function createOverlay() {
    const overlay = document.createElement("div");
    overlay.id = "bulkGPTOverlay";

    overlay.innerHTML = `
      <div id="bulkGPTHeader">
        <h3>BulkGPT</h3>
        <div id="bulkGPTIcon" title="Minimize">
          ${trashcanSVG()}
        </div>
      </div>

      <div id="bulkGPTBody">
        <div id="instructions">
          Hold <b>CTRL</b> and click chats to select multiple.
        </div>

        <div id="selectedList"></div>

        <button id="deleteBtn">Delete Selected</button>
        <button id="archiveBtn">Archive Selected</button>
      </div>
    `;

    document.body.appendChild(overlay);

    setupDrag(overlay);

    document.getElementById("deleteBtn").onclick = deleteSelected;
    document.getElementById("archiveBtn").onclick = archiveSelected;

    document.getElementById("bulkGPTIcon").onclick = () => {
      overlay.classList.toggle("minimized");
    };
  }

  function trashcanSVG() {
    return `
      <svg viewBox="0 0 24 24">
        <rect x="7" y="8" width="10" height="11" rx="2" fill="#ffa500"/>
        <rect x="9" y="10" width="2" height="7" fill="#1a1a1a"/>
        <rect x="13" y="10" width="2" height="7" fill="#1a1a1a"/>
        <rect x="6" y="6" width="12" height="2" rx="1" fill="#ffd700"/>
        <rect x="9" y="4" width="6" height="2" rx="1" fill="#ffa500"/>
      </svg>
    `;
  }

  function hookSidebarClicks() {
    document.addEventListener("click", (e) => {
      const chat = e.target.closest('a[data-sidebar-item="true"]');
      if (!chat) return;

      const href = chat.getAttribute("href") || "";
      if (!href.includes("/c/")) return;

      const id = href.split("/c/")[1].split("?")[0];
      const titleEl = chat.querySelector("span.inline-block.min-w-max");
      const title = titleEl ? titleEl.innerText.trim() : "Untitled Chat";

      const allChats = [...document.querySelectorAll('#history a[data-sidebar-item="true"]')]
        .filter(a => a.getAttribute("href")?.includes("/c/"));

      const index = allChats.indexOf(chat);

      if (e.shiftKey && lastClickedIndex !== null) {
        const [start, end] = [lastClickedIndex, index].sort((a, b) => a - b);
        for (let i = start; i <= end; i++) {
          addChat(allChats[i]);
        }
      } else {
        addChat(chat);
        lastClickedIndex = index;
      }

      updateOverlay();
      updateSidebarHighlights();
    });
  }

  function addChat(chat) {
    const href = chat.getAttribute("href");
    const id = href.split("/c/")[1].split("?")[0];
    const titleEl = chat.querySelector("span.inline-block.min-w-max");
    const title = titleEl ? titleEl.innerText.trim() : "Untitled Chat";

    if (!selectedChats.some(c => c.id === id)) {
      selectedChats.push({ id, title });
    }
  }

  function updateOverlay() {
    const list = document.getElementById("selectedList");
    list.innerHTML = "";

    selectedChats.forEach(c => {
      const div = document.createElement("div");
      div.className = "selected-item";
      div.textContent = c.title;
      list.appendChild(div);
    });
  }

  function updateSidebarHighlights() {
    const allChats = [...document.querySelectorAll('#history a[data-sidebar-item="true"]')]
      .filter(a => a.getAttribute("href")?.includes("/c/"));

    allChats.forEach(chat => chat.classList.remove("sidebar-selected"));

    selectedChats.forEach(sel => {
      const match = allChats.find(a => a.getAttribute("href").includes(sel.id));
      if (match) match.classList.add("sidebar-selected");
    });
  }

  async function deleteSelected() {
    for (const chat of selectedChats) {
      await updateChat(chat.id, { is_visible: false });
    }
    alert("Deleted selected chats.");
    selectedChats = [];
    updateOverlay();
    updateSidebarHighlights();
  }

  async function archiveSelected() {
    for (const chat of selectedChats) {
      await updateChat(chat.id, { is_archived: true });
    }
    alert("Archived selected chats.");
    selectedChats = [];
    updateOverlay();
    updateSidebarHighlights();
  }

  async function updateChat(id, payload) {
    try {
      await fetch(`https://chat.openai.com/backend-api/conversations/${id}`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      });
    } catch (e) {
      console.error("Failed to update chat:", id, e);
    }
  }

  function setupDrag(overlay) {
    const header = document.getElementById("bulkGPTHeader");

    let dragging = false;
    let startX, startY, startLeft, startTop;

    header.addEventListener("mousedown", (e) => {
      dragging = true;
      startX = e.clientX;
      startY = e.clientY;

      const rect = overlay.getBoundingClientRect();
      startLeft = rect.left;
      startTop = rect.top;

      document.addEventListener("mousemove", onMove);
      document.addEventListener("mouseup", onUp);
    });

    function onMove(e) {
      if (!dragging) return;
      overlay.style.left = `${startLeft + (e.clientX - startX)}px`;
      overlay.style.top = `${startTop + (e.clientY - startY)}px`;
    }

    function onUp() {
      dragging = false;
      document.removeEventListener("mousemove", onMove);
      document.removeEventListener("mouseup", onUp);
    }
  }

})();
""";

const string StylesCss = """
#bulkGPTOverlay {
  position: fixed;
  top: 120px;
  left: 20px;
  width: 260px;
  background: #1a1a1a;
  border: 2px solid #ffa500;
  padding: 0;
  z-index: 2147483647;
  color: white;
  font-family: Arial, sans-serif;
  border-radius: 8px;
  box-shadow: 0 4px 10px rgba(0,0,0,0.5);
}

/* Header (draggable) */
#bulkGPTHeader {
  display: flex;
  align-items: center;
  padding: 6px 8px;
  cursor: move;
  background: linear-gradient(90deg, #ffa500, #ffd700);
  border-radius: 6px 6px 0 0;
}

#bulkGPTHeader h3 {
  margin: 0;
  font-size: 14px;
  color: #000;
  flex: 1;
}

/* Trashcan icon */
#bulkGPTIcon {
  width: 22px;
  height: 22px;
  cursor: pointer;
}

#bulkGPTIcon svg {
  width: 22px;
  height: 22px;
}

/* Body */
#bulkGPTBody {
  padding: 8px;
}

#instructions {
  margin-bottom: 6px;
  font-size: 12px;
  color: #ccc;
}

#selectedList {
  max-height: 220px;
  overflow-y: auto;
  margin-bottom: 8px;
}

.selected-item {
  padding: 4px 0;
  border-bottom: 1px solid #333;
  font-size: 13px;
}

/* Neon blue highlight */
.selected-item,
.sidebar-selected {
  background: rgba(0, 150, 255, 0.25);
  border-left: 3px solid #00aaff;
  box-shadow: 0 0 8px #00aaff;
}

/* Buttons */
#deleteBtn,
#archiveBtn {
  width: 100%;
  padding: 6px;
  margin-top: 6px;
  background: #ffa500;
  border: none;
  color: black;
  font-weight: bold;
  cursor: pointer;
  border-radius: 5px;
  font-size: 12px;
}

#archiveBtn {
  background: #ffd700;
}

#deleteBtn:hover,
#archiveBtn:hover {
  background: #ffcc33;
}

/* Minimized mode */
#bulkGPTOverlay.minimized {
  width: 40px;
  height: 40px;
  padding: 0;
}

#bulkGPTOverlay.minimized #bulkGPTHeader {
  border-radius: 8px;
  padding: 8px;
}

#bulkGPTOverlay.minimized #bulkGPTHeader h3,
#bulkGPTOverlay.minimized #bulkGPTBody {
  display: none;
}
""";

const string Readme = """
# BulkGPT

BulkGPT is an open-source Chrome extension that adds a draggable overlay to ChatGPT, allowing users to bulk delete or bulk archive conversations. The extension uses a click and Shift+Click selection system that works across ChatGPT domains, including chatgpt.com and chat.openai.com.

Features
--------
- Click selection of multiple chats
- Shift+Click range selection
- Selected chats displayed inside the BulkGPT overlay
- Bulk delete selected chats
- Bulk archive selected chats
- Draggable overlay panel
- Lightweight, dependency-free implementation

How It Works
------------
BulkGPT listens for clicks on ChatGPT sidebar items. When you click a chat, it is added to the selection list. When you Shift+Click another chat, BulkGPT selects everything between the two clicks.

Selected chats appear inside the overlay, where you can delete or archive them in bulk.

Installation From This CSX
--------------------------
1. Install a C# script runner such as dotnet-script.
2. Run:
   dotnet script BulkGPT.csx
3. Open Chrome and navigate to:
   chrome://extensions
4. Enable Developer Mode.
5. Click "Load unpacked".
6. Select the generated BulkGPT folder.
7. Open ChatGPT at chatgpt.com or chat.openai.com.

Project Structure
-----------------
BulkGPT/
  manifest.json     Chrome extension manifest
  content.js        Injected script: UI, logic, selection system, API calls
  styles.css        Overlay styling

Compatibility
-------------
BulkGPT works on:
- https://chatgpt.com
- https://chat.openai.com
- Conversation pages under /c/

License
-------
BulkGPT is released under an open-source license. You may use, modify, and distribute it freely.
""";

File.WriteAllText(Path.Combine(outputDirectory, "manifest.json"), ManifestJson, Encoding.UTF8);
File.WriteAllText(Path.Combine(outputDirectory, "content.js"), ContentJs, Encoding.UTF8);
File.WriteAllText(Path.Combine(outputDirectory, "styles.css"), StylesCss, Encoding.UTF8);
File.WriteAllText(Path.Combine(outputDirectory, "README.md"), Readme, Encoding.UTF8);

Console.WriteLine($"BulkGPT extension files were written to: {outputDirectory}");
