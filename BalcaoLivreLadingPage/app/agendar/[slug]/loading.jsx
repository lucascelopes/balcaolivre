import styles from "./booking.module.css";

export default function BookingLoading() {
  return (
    <main className={styles.page} aria-busy="true">
      <div className={styles.ambientTop} />
      <section className={styles.loadingShell}>
        <div className={styles.loadingBrand} />
        <div className={styles.loadingTitle} />
        <div className={styles.loadingLine} />
        <div className={styles.loadingGrid}>
          <div />
          <div />
          <div />
          <div />
        </div>
      </section>
    </main>
  );
}
