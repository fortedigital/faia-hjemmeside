import styles from './Logo.module.scss';

function Logo({ className }) {
  return (
    <div className={`${styles.logo} ${className || ''}`}>
      <svg
        className={styles.logoSvg}
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 231.5 81.7"
        aria-label="Forte logo"
      >
        <path
          d="M158.7 65.3c0 3.8 1.6 5.4 5.4 5.4h9.4V81h-11c-10.8 0-15.5-5.2-15.5-15.2V0h11.7v65.3zM82.2 53.2c0-10.8-6.5-18.5-16.4-18.5s-16.3 7.7-16.3 18.5 6.4 18.5 16.3 18.5S82.2 64 82.2 53.2m-44.8 0c0-17 11.4-28.5 28.4-28.5s28.4 11.5 28.4 28.5c0 17.1-11.4 28.5-28.4 28.5S37.4 70.3 37.4 53.2m96.5-27.8v10.3h-7.4c-10.2 0-15 4.7-15 13.3v32H99.7V25.4h11.7v10.3c1.5-6.8 6.2-10.3 15.3-10.3h7.2zm-110.2 0H34v10.3H23.7z"
          fill="currentColor"
        />
        <path
          d="M139.601 35.76v-10.3h7.4v10.3zM19.1 15.8c0-3.8 1.6-5.4 5.4-5.4H34V0H23C12.2 0 7.4 5.2 7.4 15.2V81h11.7V15.8z"
          fill="currentColor"
        />
        <path
          d="M7.404 25.455v10.3h-7.4v-10.3zM219.3 47.1c-1-8.3-6.7-12.9-14.9-12.9-7.5 0-13.9 5-14.8 12.9h29.7zM177 53.3c0-17.2 11-28.6 27.4-28.6 16.1 0 26.8 10.2 27.2 26.5 0 1.4-.1 2.9-.3 4.5h-42v.8c.3 9.5 6.3 15.7 15.5 15.7 7.2 0 12.4-3.6 14-9.8h11.7c-2 11-11.3 19.3-25.1 19.3-17.5 0-28.4-11.3-28.4-28.4m-14.4-22.7c0-3.2 2.6-5.8 5.8-5.8 3.2 0 5.8 2.6 5.8 5.8s-2.6 5.8-5.8 5.8c-3.2 0-5.8-2.6-5.8-5.8"
          fill="currentColor"
        />
      </svg>
      <span className={styles.divider} />
      <span className={styles.subtitle}>AI Accelerator</span>
    </div>
  );
}

export default Logo;
