"use client";

import { useEffect } from "react";
import styles from "./agenda-livre.module.css";

export default function AgendaLivreMotion() {
  useEffect(() => {
    const root = document.querySelector("[data-agenda-landing]");

    if (!root) return undefined;

    const revealItems = Array.from(root.querySelectorAll("[data-reveal]"));
    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    if (reducedMotion) {
      revealItems.forEach((item) => item.classList.add(styles.revealVisible));
      return undefined;
    }

    const isAlreadyVisible = (item) => {
      const bounds = item.getBoundingClientRect();
      return bounds.top < window.innerHeight * 0.94 && bounds.bottom > 0;
    };

    root.classList.add(styles.motionReady);
    const initiallyVisible = new Set(revealItems.filter(isAlreadyVisible));
    let introFrame = window.requestAnimationFrame(() => {
      introFrame = window.requestAnimationFrame(() => {
        initiallyVisible.forEach((item) => item.classList.add(styles.revealVisible));
      });
    });

    if (!("IntersectionObserver" in window)) {
      let frame = null;

      const revealVisibleItems = () => {
        frame = null;
        revealItems.forEach((item) => {
          if (isAlreadyVisible(item)) item.classList.add(styles.revealVisible);
        });
      };

      const handleViewportChange = () => {
        if (frame !== null) return;
        frame = window.requestAnimationFrame(revealVisibleItems);
      };

      window.addEventListener("scroll", handleViewportChange, { passive: true });
      window.addEventListener("resize", handleViewportChange);

      return () => {
        window.cancelAnimationFrame(introFrame);
        if (frame !== null) window.cancelAnimationFrame(frame);
        window.removeEventListener("scroll", handleViewportChange);
        window.removeEventListener("resize", handleViewportChange);
      };
    }

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (!entry.isIntersecting) return;
          entry.target.classList.add(styles.revealVisible);
          observer.unobserve(entry.target);
        });
      },
      { rootMargin: "0px 0px -8% 0px", threshold: 0.08 },
    );

    revealItems.forEach((item) => {
      if (!initiallyVisible.has(item)) observer.observe(item);
    });

    return () => {
      window.cancelAnimationFrame(introFrame);
      observer.disconnect();
    };
  }, []);

  return null;
}
