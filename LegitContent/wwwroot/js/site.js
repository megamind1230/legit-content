const dialog = document.getElementById("add-dialog");
document.getElementById("add-button")?.addEventListener("click", () => dialog?.showModal());

const navDialog = document.getElementById("nav-dialog");
document.getElementById("nav-button")?.addEventListener("click", () => navDialog?.showModal());

document.querySelectorAll("[data-close]").forEach((button) =>
	button.addEventListener("click", () => button.closest("dialog")?.close())
);

const csrfToken = document.querySelector('meta[name="csrf-token"]')?.content;
document.querySelectorAll(".vote form").forEach((form) => {
	form.addEventListener("submit", async (event) => {
		if (!csrfToken) return;
		event.preventDefault();
		try {
			const response = await fetch(form.action, {
				method: "POST",
				headers: {
					"X-Requested-With": "fetch",
					"RequestVerificationToken": csrfToken,
				},
				body: new FormData(form),
			});
			if (!response.ok) {
				form.submit();
				return;
			}
			const result = await response.json();
			const button = form.querySelector(".vote-btn");
			button.classList.toggle("voted", result.voted);
			button.textContent = `▲ ${result.upvotes}`;
		} catch {
			form.submit();
		}
	});
});

async function postAction(form) {
	const response = await fetch(form.action, {
		method: "POST",
		headers: {
			"X-Requested-With": "fetch",
			"RequestVerificationToken": csrfToken,
		},
		body: new FormData(form),
	});
	if (!response.ok) throw new Error(`Request failed: ${response.status}`);
	return response.json();
}

document.querySelectorAll(".action-form.read, .action-form.unread, .action-form.delete").forEach((form) => {
	form.addEventListener("submit", async (event) => {
		if (!csrfToken) return;
		event.preventDefault();
		try {
			await postAction(form);
			const target = form.closest(".card, .archive-item");
			if (!target) return;
			target.classList.add("removing");
			setTimeout(() => target.remove(), 150);
		} catch {
			form.submit();
		}
	});
});

document.querySelectorAll(".action-form.archive").forEach((form) => {
	form.addEventListener("submit", async (event) => {
		if (!csrfToken) return;
		event.preventDefault();
		try {
			await postAction(form);
			form.querySelector(".icon-action")?.classList.add("saved");
		} catch {
			form.submit();
		}
	});
});

function debounce(fn, wait) {
	let timer;
	return (...args) => {
		clearTimeout(timer);
		timer = setTimeout(() => fn(...args), wait);
	};
}

async function fetchTagSuggestions(query) {
	const response = await fetch(`/tags?q=${encodeURIComponent(query)}`);
	return response.ok ? await response.json() : [];
}

function normalizeTagPath(value) {
	return value
		.toLowerCase()
		.split("/")
		.map((segment) => segment.trim().replace(/\s+/g, "-"))
		.filter(Boolean)
		.join("/");
}

function renderSuggestions(list, items, onPick) {
	list.innerHTML = "";
	if (items.length === 0) {
		list.hidden = true;
		return;
	}
	for (const item of items) {
		const li = document.createElement("li");
		li.textContent = item.path;
		li.dataset.path = item.path;
		const count = document.createElement("span");
		count.className = "count";
		count.textContent = item.linkCount;
		li.appendChild(count);
		li.addEventListener("mousedown", (event) => {
			event.preventDefault();
			onPick(item.path);
		});
		list.appendChild(li);
	}
	list.hidden = false;
}

function closeSuggestions(list) {
	list.hidden = true;
	list.innerHTML = "";
}

const searchDialog = document.getElementById("search-dialog");
const searchButton = document.getElementById("search-button");
const tagPanel = document.getElementById("tag-panel");
const filtersToggle = document.getElementById("filters-toggle");
const tagInput = document.getElementById("tag-input");
const tagSuggestions = document.getElementById("tag-suggestions");
const tagChips = document.getElementById("tag-chips");
const overlayTags = document.getElementById("overlay-tags");
const overlaySearch = document.getElementById("overlay-search");
const overlayType = document.getElementById("overlay-type");

let selectedTags = [];
let suggestionIndex = -1;

function syncTagChips() {
	tagChips.innerHTML = "";
	for (const tag of selectedTags) {
		const chip = document.createElement("span");
		chip.className = "chip removable";
		chip.textContent = tag;
		const x = document.createElement("span");
		x.className = "x";
		x.textContent = "×";
		x.addEventListener("click", () => {
			selectedTags = selectedTags.filter((t) => t !== tag);
			syncTagChips();
		});
		chip.appendChild(x);
		tagChips.appendChild(chip);
	}
	overlayTags.value = selectedTags.join(",");
}

function commitTag(value) {
	const path = normalizeTagPath(value);
	if (!path || selectedTags.includes(path) || selectedTags.length >= 5) return;
	selectedTags.push(path);
	tagInput.value = "";
	syncTagChips();
}

searchButton?.addEventListener("click", () => {
	if (!searchDialog) return;
	const params = new URLSearchParams(location.search);
	overlaySearch.value = params.get("Search") ?? "";
	overlayType.value = params.get("Type") ?? "";
	selectedTags = (params.get("Tags") ?? "").split(",").map((t) => t.trim()).filter(Boolean);
	syncTagChips();
	tagPanel.hidden = selectedTags.length === 0;
	for (const radio of searchDialog.querySelectorAll('input[name="Sort"]'))
		radio.checked = radio.value === (params.get("Sort") || "MostLegit");
	for (const radio of searchDialog.querySelectorAll('input[name="TagMode"]'))
		radio.checked = radio.value === (params.get("TagMode") || "Any");
	searchDialog.showModal();
	overlaySearch.focus();
});

filtersToggle?.addEventListener("click", () => {
	if (!tagPanel) return;
	tagPanel.hidden = !tagPanel.hidden;
	if (!tagPanel.hidden) tagInput.focus();
});

tagInput?.addEventListener("keydown", (event) => {
	const items = [...tagSuggestions.querySelectorAll("li")];
	if (event.key === "Enter") {
		event.preventDefault();
		if (suggestionIndex >= 0 && items[suggestionIndex]) commitTag(items[suggestionIndex].dataset.path);
		else commitTag(tagInput.value);
		closeSuggestions(tagSuggestions);
	} else if (event.key === "ArrowDown" && items.length > 0) {
		event.preventDefault();
		suggestionIndex = (suggestionIndex + 1) % items.length;
		items.forEach((item, i) => item.classList.toggle("active", i === suggestionIndex));
	} else if (event.key === "ArrowUp" && items.length > 0) {
		event.preventDefault();
		suggestionIndex = (suggestionIndex - 1 + items.length) % items.length;
		items.forEach((item, i) => item.classList.toggle("active", i === suggestionIndex));
	} else if (event.key === "Escape") {
		closeSuggestions(tagSuggestions);
	}
});

const searchTags = debounce(async () => {
	suggestionIndex = -1;
	const query = tagInput.value.trim().split("/").pop().trim();
	if (query.length === 0) {
		closeSuggestions(tagSuggestions);
		return;
	}
	const items = await fetchTagSuggestions(query);
	renderSuggestions(tagSuggestions, items, (path) => {
		commitTag(path);
		closeSuggestions(tagSuggestions);
		tagInput.focus();
	});
}, 200);
tagInput?.addEventListener("input", searchTags);

const addTagsInput = document.getElementById("NewLink_Tags");
const addTagSuggestions = document.getElementById("add-tag-suggestions");
let addSuggestionIndex = -1;

addTagsInput?.addEventListener("keydown", (event) => {
	const items = [...addTagSuggestions.querySelectorAll("li")];
	if (event.key === "Enter" && items.length > 0) {
		event.preventDefault();
		if (addSuggestionIndex >= 0 && items[addSuggestionIndex]) {
			const segments = addTagsInput.value.split(",");
			segments[segments.length - 1] = items[addSuggestionIndex].dataset.path;
			addTagsInput.value = segments.map((s) => s.trim()).filter(Boolean).join(",") + ",";
		}
		closeSuggestions(addTagSuggestions);
		addTagsInput.focus();
	}
});

const searchAddTags = debounce(async () => {
	addSuggestionIndex = -1;
	const segments = addTagsInput.value.split(",");
	const query = segments[segments.length - 1].trim().split("/").pop().trim();
	if (query.length === 0 || segments.length > 5) {
		closeSuggestions(addTagSuggestions);
		return;
	}
	const items = await fetchTagSuggestions(query);
	renderSuggestions(addTagSuggestions, items, (path) => {
		segments[segments.length - 1] = path;
		addTagsInput.value = segments.map((s) => s.trim()).filter(Boolean).join(",") + ",";
		closeSuggestions(addTagSuggestions);
		addTagsInput.focus();
	});
}, 200);
addTagsInput?.addEventListener("input", searchAddTags);

document.addEventListener("click", (event) => {
	if (!event.target.closest(".tag-input-wrap")) closeSuggestions(tagSuggestions);
	if (!event.target.closest("#NewLink_Tags")) closeSuggestions(addTagSuggestions);
});

document.getElementById("theme-toggle")?.addEventListener("click", () => {
	const root = document.documentElement;
	const next = root.dataset.theme === "dark" ? "light" : "dark";
	root.dataset.theme = next;
	localStorage.setItem("theme", next);
});
