export function init(elem) {
    elem.focus();

    // Auto-resize whenever the user types or if the value is set programmatically
    elem.addEventListener('input', () => resizeToFit(elem));
    afterPropertyWritten(elem, 'value', () => resizeToFit(elem));

    // Auto-submit the form on 'enter' keypress
    elem.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            elem.dispatchEvent(new CustomEvent('change', { bubbles: true }));
            elem.closest('form').dispatchEvent(new CustomEvent('submit', { bubbles: true, cancelable: true }));
        }
    });
}

function resizeToFit(elem) {
    const lineHeight = parseFloat(getComputedStyle(elem).lineHeight);

    elem.rows = 1;
    const numLines = Math.ceil(elem.scrollHeight / lineHeight);
    elem.rows = Math.min(5, Math.max(1, numLines));
}

function afterPropertyWritten(elem, propertyName, callback) {
    const originalPropertyDescriptor = Object.getOwnPropertyDescriptor(elem.constructor.prototype, propertyName) || Object.getOwnPropertyDescriptor(HTMLElement.prototype, propertyName);
    
    if (originalPropertyDescriptor && originalPropertyDescriptor.set) {
        const originalSetter = originalPropertyDescriptor.set;
        const newSetter = function (value) {
            originalSetter.call(this, value);
            callback();
        };
        
        Object.defineProperty(elem, propertyName, {
            get: originalPropertyDescriptor.get,
            set: newSetter,
            enumerable: true,
            configurable: true
        });
    }
}
