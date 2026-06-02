document.getElementById('sidebarToggle')?.addEventListener('click', () => {
    document.getElementById('sidebar').classList.toggle('open');
});

document.querySelectorAll('.alert').forEach(a => {
    setTimeout(() => { try { new bootstrap.Alert(a).close(); } catch (e) { } }, 4000);
});

document.querySelectorAll('.code-box').forEach((box, i, boxes) => {
    box.addEventListener('input', e => {
        e.target.value = e.target.value.replace(/\D/g, '').slice(-1);
        if (e.target.value && i < boxes.length - 1) boxes[i + 1].focus();
        syncCode(boxes);
    });
    box.addEventListener('keydown', e => {
        if (e.key === 'Backspace' && !box.value && i > 0) boxes[i - 1].focus();
    });
    box.addEventListener('paste', e => {
        e.preventDefault();
        const p = (e.clipboardData || window.clipboardData).getData('text').replace(/\D/g, '');
        p.split('').slice(0, 6).forEach((c, j) => { if (boxes[j]) boxes[j].value = c; });
        syncCode(boxes);
        boxes[Math.min(p.length, boxes.length - 1)].focus();
    });
});

function syncCode(boxes) {
    const h = document.getElementById('codeInput');
    if (h) h.value = Array.from(boxes).map(b => b.value).join('');
}

document.querySelectorAll('.btn-delete').forEach(btn => {
    btn.addEventListener('click', e => {
        if (!confirm('Delete this record? This cannot be undone.')) e.preventDefault();
    });
});