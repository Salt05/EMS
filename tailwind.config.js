/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/EMS.BlazorWASM/**/*.{razor,html,cshtml}"
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'sans-serif'],
        mono: ['JetBrains Mono', 'SF Mono', 'monospace'],
      },
      colors: {
        canvas: '#F7F9FC',
        surface: '#FFFFFF',
        border: '#E3E8EE',
        charcoal: '#1A1F36',
        muted: '#787774',
        accent: '#0A2540',
        paleRed: { bg: '#FDEBEC', text: '#9F2F2D' },
        paleBlue: { bg: '#E1F3FE', text: '#1F6C9F' },
        paleGreen: { bg: '#EDF3EC', text: '#346538' },
        paleYellow: { bg: '#FBF3DB', text: '#956400' }
      },
      boxShadow: {
        'bento': '0 2px 8px rgba(26, 31, 54, 0.02)',
        'bento-hover': '0 4px 16px rgba(26, 31, 54, 0.04)',
      }
    }
  },
  plugins: [],
}
