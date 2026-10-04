import { useCallback } from 'react';

export function useSoundAlert() {
  const playAlert = useCallback(() => {
    try {
      const audioContext = new (window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext)();
      
      const playTone = (startTime: number, duration: number) => {
        const oscillator = audioContext.createOscillator();
        const gainNode = audioContext.createGain();
        
        oscillator.connect(gainNode);
        gainNode.connect(audioContext.destination);
        
        oscillator.frequency.value = 800;
        oscillator.type = 'sine';
        gainNode.gain.value = 0.3;
        
        oscillator.start(startTime);
        oscillator.stop(startTime + duration);
      };

      playTone(0, 0.3);
      playTone(0.4, 0.3);
      playTone(0.8, 0.3);
    } catch (error) {
      console.error('Error playing sound:', error);
    }
  }, []);

  return { playAlert };
}
