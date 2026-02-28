(() => {
  const lobbyView = document.getElementById("lobbyView");
  const courseView = document.getElementById("courseView");

  const brandSubEl = document.getElementById("brandSub");
  const addCourseBtn = document.getElementById("addCourseBtn");
  const backBtn = document.getElementById("backBtn");
  const filterEl = document.getElementById("filter");
  const reloadBtn = document.getElementById("reloadBtn");

  const lobbyMetaEl = document.getElementById("lobbyMeta");
  const lobbyEmptyEl = document.getElementById("lobbyEmpty");
  const lobbyErrEl = document.getElementById("lobbyErr");
  const coursesGridEl = document.getElementById("coursesGrid");

  const outEl = document.getElementById("out");
  const errEl = document.getElementById("err");
  const statsEl = document.getElementById("stats");

  const player = document.getElementById("player");
  const nowTitle = document.getElementById("nowTitle");
  const nowMeta = document.getElementById("nowMeta");
  const resumeHint = document.getElementById("resumeHint");
  const progressLine = document.getElementById("progressLine");
  const markDoneBtn = document.getElementById("markDoneBtn");
  const clearBtn = document.getElementById("clearBtn");

  const STORAGE_KEY = "tg_progress_v5";
  const LAST_KEY = "tg_last_v5";

  const loadJSON = (key, fallback) => {
    try {
      const raw = localStorage.getItem(key);
      return raw ? JSON.parse(raw) : fallback;
    } catch {
      return fallback;
    }
  };

  const saveJSON = (key, value) => {
    try {
      localStorage.setItem(key, JSON.stringify(value));
    } catch {
      // ignore
    }
  };

  let progress = loadJSON(STORAGE_KEY, {});
  let lastByCourse = loadJSON(LAST_KEY, {});

  let courses = [];
  let currentCourse = null;
  let sections = [];
  let current = null;
  let isLoadingLesson = false;
  let lastSaveMs = 0;
  let suppressNextForcedSave = false;
  let activeMetaToken = 0;

  function clearErrors() {
    errEl.textContent = "";
    lobbyErrEl.textContent = "";
  }

  function setError(e) {
    const msg = String(e && e.stack ? e.stack : e);
    errEl.textContent = msg;
    lobbyErrEl.textContent = msg;
  }

  async function fetchJSON(url, options) {
    const res = await fetch(url, options);
    if (!res.ok) throw new Error(`${url} failed: ${res.status} ${res.statusText}`);
    return await res.json();
  }

  const formatTime = (sec) => {
    if (!isFinite(sec) || sec < 0) return "0:00";
    const s = Math.floor(sec);
    const m = Math.floor(s / 60);
    const r = s % 60;
    return `${m}:${String(r).padStart(2, "0")}`;
  };

  const clampTime = (t, dur) => {
    if (!isFinite(t) || t < 0) t = 0;
    if (!isFinite(dur) || dur <= 0) return t;
    return Math.min(t, Math.max(0, dur - 0.35));
  };

  const getProg = (url) => progress[url] || null;

  const setProg = (url, patch) => {
    const prev = progress[url] || { time: 0, duration: 0, completed: false, updatedAt: 0 };
    const next = { ...prev, ...patch, updatedAt: Date.now() };
    progress[url] = next;
    saveJSON(STORAGE_KEY, progress);
  };

  const setLast = (courseId, url) => {
    if (!courseId || !url) return;
    lastByCourse[courseId] = { url };
    saveJSON(LAST_KEY, lastByCourse);
  };

  const countWatched = () => {
    let total = 0;
    let watched = 0;
    for (const sec of sections) {
      const vids = sec.videos || [];
      total += vids.length;
      for (const v of vids) if (progress[v.url]?.completed) watched++;
    }
    return { total, watched };
  };

  function showLobby() {
    lobbyView.hidden = false;
    courseView.hidden = true;
    backBtn.hidden = true;
    filterEl.hidden = true;
    reloadBtn.hidden = true;
    filterEl.value = "";

    try {
      player.pause();
      player.removeAttribute("src");
      player.load();
    } catch {
      // ignore
    }

    currentCourse = null;
    sections = [];
    current = null;
    updatePlayerHeaderUI();
  }

  function showCourseView() {
    lobbyView.hidden = true;
    courseView.hidden = false;
    backBtn.hidden = false;
    filterEl.hidden = false;
    reloadBtn.hidden = false;
  }

  async function addCourseWithPicker() {
    let json;
    try {
      json = await fetchJSON("/api/courses/pick", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: "{}",
      });
    } catch (e) {
      const msg = String(e && e.message ? e.message : e);
      const canFallback = msg.includes(" 501 ") || msg.includes(" 404 ");
      if (!canFallback) throw e;
      json = await fetchJSON("/api/courses/pick", { cache: "no-store" });
    }

    if (json.cancelled) return;
    await loadCourses();
    if (json.courseId) await openCourse(json.courseId);
  }

  async function removeCourse(course) {
    const label = course?.title || "this course";
    const ok = window.confirm(`Remove "${label}" from the lobby?\n\nThis will NOT delete the folder or videos.`);
    if (!ok) return;

    try {
      await fetchJSON("/api/courses/remove", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ id: course.id }),
      });
    } catch (e) {
      const msg = String(e && e.message ? e.message : e);
      const canFallback = msg.includes(" 501 ") || msg.includes(" 404 ");
      if (!canFallback) throw e;
      await fetchJSON(`/api/courses/remove?id=${encodeURIComponent(course.id)}`, { cache: "no-store" });
    }

    if (currentCourse?.id === course.id) showLobby();
    await loadCourses();
  }

  function renderLobby() {
    clearErrors();
    coursesGridEl.innerHTML = "";

    const active = courses.filter((c) => c.exists);
    lobbyMetaEl.textContent = `${courses.length} total, ${active.length} available`;
    brandSubEl.textContent = courses.length ? `${courses.length} courses added` : "No courses added yet";
    lobbyEmptyEl.hidden = courses.length !== 0;

    for (const c of courses) {
      const card = document.createElement("div");
      card.className = "courseCard";
      if (!c.exists) card.classList.add("missing");

      const thumb = document.createElement("div");
      thumb.className = "courseThumb";

      if (c.firstVideoUrl && c.exists) {
        const v = document.createElement("video");
        v.muted = true;
        v.preload = "metadata";
        v.src = c.firstVideoUrl;
        v.className = "courseThumbVideo";
        v.addEventListener("loadedmetadata", () => {
          try {
            v.currentTime = Math.min(1, Math.max(0, (v.duration || 0) * 0.02));
          } catch {
            // ignore
          }
        });
        thumb.appendChild(v);
      } else {
        const p = document.createElement("div");
        p.className = "courseThumbPlaceholder";
        p.textContent = c.exists ? "No preview" : "Missing folder";
        thumb.appendChild(p);
      }

      const body = document.createElement("div");
      body.className = "courseBody";

      const title = document.createElement("div");
      title.className = "courseTitle";
      title.textContent = c.title || "Untitled";

      const meta = document.createElement("div");
      meta.className = "courseMeta";
      meta.textContent = c.exists ? `${c.sectionCount || 0} sections, ${c.videoCount || 0} videos` : "Folder not found";

      const path = document.createElement("div");
      path.className = "coursePath";
      path.textContent = c.path || "";

      const actions = document.createElement("div");
      actions.className = "courseActions";

      const openBtn = document.createElement("button");
      openBtn.type = "button";
      openBtn.className = "btnGhost courseActionBtn";
      openBtn.textContent = "Open";
      openBtn.disabled = !c.exists;
      openBtn.addEventListener("click", () => {
        if (!c.exists) return;
        openCourse(c.id).catch(setError);
      });

      const removeBtn = document.createElement("button");
      removeBtn.type = "button";
      removeBtn.className = "btnGhost courseActionBtn courseActionDanger";
      removeBtn.textContent = "Delete";
      removeBtn.addEventListener("click", () => removeCourse(c).catch(setError));

      actions.appendChild(openBtn);
      actions.appendChild(removeBtn);

      body.appendChild(title);
      body.appendChild(meta);
      body.appendChild(path);
      body.appendChild(actions);

      card.appendChild(thumb);
      card.appendChild(body);
      coursesGridEl.appendChild(card);
    }
  }

  async function loadCourses() {
    const json = await fetchJSON("/api/courses", { cache: "no-store" });
    courses = json.courses || [];
    renderLobby();
  }

  function updatePlayerHeaderUI() {
    if (!current) {
      nowTitle.textContent = "Select a lesson";
      nowMeta.textContent = "";
      resumeHint.textContent = "";
      progressLine.textContent = "";
      markDoneBtn.disabled = true;
      clearBtn.disabled = true;
      return;
    }

    const p = getProg(current.url);
    const completed = !!p?.completed;
    const t = p?.time || 0;
    const d = p?.duration || 0;

    nowTitle.textContent = current.title;
    nowMeta.textContent = `Section #${current.sectionOrder} - ${current.sectionTitle}`;
    resumeHint.textContent = t > 1 && !completed ? `Resume at ${formatTime(t)}` : completed ? "Watched" : "";

    if (completed) {
      progressLine.textContent = "Status: watched";
    } else if (d > 0) {
      const pct = Math.round(Math.min(100, Math.max(0, (t / d) * 100)));
      progressLine.textContent = `Progress: ${formatTime(t)} / ${formatTime(d)} (${pct}%)`;
    } else {
      progressLine.textContent = t > 0 ? `Progress: ${formatTime(t)}` : "";
    }

    markDoneBtn.disabled = false;
    clearBtn.disabled = false;
  }

  function renderCurriculum() {
    outEl.innerHTML = "";
    clearErrors();

    const q = (filterEl.value || "").trim().toLowerCase();
    const { total, watched } = countWatched();
    statsEl.textContent = `${watched}/${total} watched`;

    for (const sec of sections) {
      const secHay = `${sec.order} ${sec.title} ${sec.folder}`.toLowerCase();
      const lessons = (sec.videos || []).filter((v) => {
        if (!q) return true;
        return secHay.includes(q) || v.title.toLowerCase().includes(q);
      });

      if (q && !secHay.includes(q) && lessons.length === 0) continue;

      const secEl = document.createElement("div");
      secEl.className = "section open";

      const head = document.createElement("div");
      head.className = "sectionHead";

      const num = document.createElement("span");
      num.className = "sectionNum";
      num.textContent = `#${sec.order}`;

      const title = document.createElement("span");
      title.className = "sectionTitle";
      title.textContent = sec.title;

      const meta = document.createElement("span");
      meta.className = "sectionMeta";
      meta.textContent = `${lessons.length} lesson${lessons.length === 1 ? "" : "s"}`;

      head.appendChild(num);
      head.appendChild(title);
      head.appendChild(meta);
      head.addEventListener("click", () => secEl.classList.toggle("open"));

      const body = document.createElement("div");
      body.className = "sectionBody";

      for (const v of lessons) {
        const p = getProg(v.url);
        const completed = !!p?.completed;
        const dur = p?.duration || 0;
        const t = p?.time || 0;

        const row = document.createElement("div");
        row.className = "lesson";
        if (current?.url === v.url) row.classList.add("active");
        if (completed) row.classList.add("watched");

        const text = document.createElement("div");
        text.className = "lessonTitle";
        text.textContent = v.title;

        const right = document.createElement("div");
        right.className = "lessonRight";

        const rightText = document.createElement("span");
        rightText.textContent = completed ? "watched" : dur ? `${formatTime(t)}/${formatTime(dur)}` : t ? formatTime(t) : "";
        right.appendChild(rightText);

        if (completed) {
          const check = document.createElement("span");
          check.className = "lessonCheckmark";
          check.textContent = "\u2713";
          right.appendChild(check);
        }

        row.appendChild(text);
        row.appendChild(right);

        row.addEventListener("click", () => {
          loadLesson({
            url: v.url,
            title: v.title,
            sectionTitle: sec.title,
            sectionOrder: sec.order,
          });
        });

        body.appendChild(row);
      }

      secEl.appendChild(head);
      secEl.appendChild(body);
      outEl.appendChild(secEl);
    }

    updatePlayerHeaderUI();
  }

  async function loadLesson(info) {
    if (current && current.url && current.url !== info.url) persistProgress(true);

    current = info;
    if (currentCourse?.id) setLast(currentCourse.id, info.url);
    renderCurriculum();

    isLoadingLesson = true;
    player.pause();
    player.removeAttribute("src");
    player.load();

    player.src = info.url;
    player.load();

    const url = info.url;
    const saved = getProg(url);
    const desired = saved?.time || 0;
    const metaToken = ++activeMetaToken;

    const onMeta = () => {
      if (metaToken !== activeMetaToken) return;
      if (!current || current.url !== url) return;

      const dur = isFinite(player.duration) ? player.duration : 0;
      setProg(url, { duration: dur });

      const target = clampTime(desired, dur);
      if (target > 0.2) {
        try { player.currentTime = target; } catch {}
      }

      setTimeout(() => {
        if (metaToken !== activeMetaToken) return;
        if (!current || current.url !== url) return;
        isLoadingLesson = false;
        updatePlayerHeaderUI();
        renderCurriculum();
      }, 150);
    };

    player.addEventListener("loadedmetadata", onMeta, { once: true });
  }

  function persistProgress(force = false) {
    if (!current) return;
    if (force && suppressNextForcedSave) {
      suppressNextForcedSave = false;
      return;
    }
    if (!isFinite(player.currentTime)) return;
    if (isLoadingLesson && !force) return;

    const now = Date.now();
    if (!force && now - lastSaveMs < 900) return;
    lastSaveMs = now;

    const url = current.url;
    const dur = isFinite(player.duration) ? player.duration : getProg(url)?.duration || 0;
    const time = player.currentTime;
    const completed = dur > 0 && time / dur >= 0.95;

    setProg(url, { time: completed ? dur : time, duration: dur, completed });
    updatePlayerHeaderUI();
  }

  function markWatched() {
    if (!current) return;
    const url = current.url;
    const dur = isFinite(player.duration) ? player.duration : getProg(url)?.duration || 0;
    setProg(url, { completed: true, time: dur || (getProg(url)?.time || 0), duration: dur });
    renderCurriculum();
  }

  function clearProgress() {
    if (!current) return;
    const url = current.url;
    delete progress[url];
    saveJSON(STORAGE_KEY, progress);

    try {
      suppressNextForcedSave = true;
      player.pause();
      player.currentTime = 0;
    } catch {
      // ignore
    }

    updatePlayerHeaderUI();
    renderCurriculum();
  }

  async function openCourse(courseId) {
    clearErrors();
    const target = courses.find((c) => c.id === courseId);
    if (!target || !target.exists) throw new Error("Course is not available.");

    currentCourse = target;
    showCourseView();

    const json = await fetchJSON(`/api/courses/${courseId}/index.json`, { cache: "no-store" });
    sections = json.items || [];
    if (!sections.length) throw new Error("Selected course has no sections with videos.");

    brandSubEl.textContent = `Course: ${target.title}`;

    let defaultLesson = null;
    const last = lastByCourse[courseId];
    if (last?.url) {
      for (const sec of sections) {
        for (const v of sec.videos || []) {
          if (v.url === last.url) {
            defaultLesson = { url: v.url, title: v.title, sectionTitle: sec.title, sectionOrder: sec.order };
            break;
          }
        }
        if (defaultLesson) break;
      }
    }

    if (!defaultLesson) {
      const firstSec = sections[0];
      const firstVid = firstSec?.videos?.[0];
      if (firstVid) {
        defaultLesson = {
          url: firstVid.url,
          title: firstVid.title,
          sectionTitle: firstSec.title,
          sectionOrder: firstSec.order,
        };
      }
    }

    renderCurriculum();
    if (defaultLesson) await loadLesson(defaultLesson);
  }

  addCourseBtn.addEventListener("click", async () => {
    try {
      await addCourseWithPicker();
    } catch (e) {
      setError(e);
      showLobby();
    }
  });

  backBtn.addEventListener("click", async () => {
    persistProgress(true);
    showLobby();
    try {
      await loadCourses();
    } catch (e) {
      setError(e);
    }
  });

  reloadBtn.addEventListener("click", async () => {
    if (!currentCourse?.id) return;
    try {
      await openCourse(currentCourse.id);
    } catch (e) {
      setError(e);
    }
  });

  filterEl.addEventListener("input", renderCurriculum);
  markDoneBtn.addEventListener("click", markWatched);
  clearBtn.addEventListener("click", clearProgress);

  player.addEventListener("timeupdate", () => persistProgress(false));
  player.addEventListener("pause", () => persistProgress(true));
  player.addEventListener("seeked", () => persistProgress(true));

  player.addEventListener("ended", () => {
    if (!current) return;
    const url = current.url;
    const dur = isFinite(player.duration) ? player.duration : getProg(url)?.duration || 0;
    setProg(url, { completed: true, time: dur || player.currentTime || 0, duration: dur });
    renderCurriculum();
  });

  document.addEventListener("visibilitychange", () => {
    if (document.visibilityState === "hidden") persistProgress(true);
  });
  window.addEventListener("pagehide", () => persistProgress(true));
  document.addEventListener("freeze", () => persistProgress(true));
  window.addEventListener("beforeunload", () => persistProgress(true));

  setInterval(() => {
    if (!current) return;
    if (player.paused) return;
    persistProgress(false);
  }, 1500);

  window.addEventListener(
    "keydown",
    (e) => {
      const isSpace = e.code === "Space" || e.key === " ";
      if (!isSpace) return;

      const t = e.target;
      const tag = t && t.tagName ? t.tagName.toLowerCase() : "";
      const isTyping =
        tag === "input" ||
        tag === "textarea" ||
        tag === "select" ||
        (t && t.isContentEditable);
      if (isTyping) return;
      if (e.altKey || e.ctrlKey || e.metaKey) return;

      e.preventDefault();
      if (!currentCourse || !current || !player.src) return;

      if (player.paused) player.play().catch(() => {});
      else player.pause();
    },
    { capture: true }
  );

  (async () => {
    try {
      showLobby();
      await loadCourses();
    } catch (e) {
      setError(e);
    }
  })();
})();
